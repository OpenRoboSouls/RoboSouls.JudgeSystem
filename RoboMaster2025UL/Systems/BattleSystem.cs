using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RoboSouls.JudgeSystem.Entities;
using RoboSouls.JudgeSystem.Events;
using RoboSouls.JudgeSystem.Systems;
using VContainer;
using VitalRouter;

namespace RoboSouls.JudgeSystem.RoboMaster2025UL.Systems;

/// <summary>
/// 射击、伤害、热量系统
/// </summary>
public sealed class BattleSystem : IBattleSystem
{
    private static readonly int RedDamageSumCacheKey = "RedDamageSum".GetHashCode();
    private static readonly int BlueDamageSumCacheKey = "BlueDamageSum".GetHashCode();

    [Inject]
    internal ICommandPublisher Publisher { get; set; }

    [Inject]
    internal ICacheProvider<int> AmmoBox { get; set; }

    [Inject]
    internal LifeSystem LifeSystem { get; set; }

    [Inject]
    internal ITimeSystem TimeSystem { get; set; }

    [Inject]
    internal RM2025ulPerformanceSystem PerformanceSystem { get; set; }

    [Inject]
    internal EntitySystem EntitySystem { get; set; }

    [Inject]
    internal ILogger Logger { get; set; }

    [Inject]
    internal BuffSystem BuffSystem { get; set; }

    [Inject]
    internal ICacheWriter<int> IntCacheBox { get; set; }

    [Inject]
    internal ICacheWriter<float> FloatCacheBox { get; set; }

    [Inject]
    internal ICacheProvider<uint> UintCacheBox { get; set; }

    [Inject]
    internal ModuleSystem ModuleSystem { get; set; }

    // use delegate instead of event to avoid redundant routing
    internal Action<IShooter, int> OnShoot { get; set; } = delegate { };

    public Task Reset(CancellationToken cancellation = new CancellationToken())
    {
        TimeSystem.RegisterRepeatAction(0.1, CooldownTask);

        return Task.CompletedTask;
    }

    public bool TryShoot(IShooter shooter, int count)
    {
        if (shooter.AmmoAllowance <= 0)
        {
            return false;
        }

        // 允许一定超发
        SetAmmoAllowance(shooter, shooter.AmmoAllowance - count);
        SetHeat(shooter, shooter.Heat + PerformanceSystem.GetHeatDelta(shooter) * count);

        OnShoot(shooter, count);

        return true;
    }

    private Task CooldownTask()
    {
        return Task.WhenAll(
            EntitySystem
                .Entities.Values.OfType<IShooter>()
                .Where(EntitySystem.HasOperator)
                .Select(CooldownTaskFor)
        );
    }

    /// <summary>
    /// 热量冷却结算 10Hz
    /// 枪口热量按 10Hz 的频率结算冷却，每个检测周期热量冷却值 = 每秒
    /// 冷却值 / 10。
    /// </summary>
    /// <param name="shooter"></param>
    private Task CooldownTaskFor(IShooter shooter)
    {
        if (shooter is IHealthed h)
        {
            if (h.IsDead())
            {
                SetHeat(shooter, 0);
                return Task.CompletedTask;
            }
        }

        var cooldown = PerformanceSystem.GetCooldown(shooter);
        var cooldownDelta = (float)cooldown / 10;
        if (BuffSystem.TryGetBuff(shooter.Id, Buffs.CoolDownBuff, out float cooldownBuff))
        {
            cooldownDelta *= cooldownBuff;
        }

        var q1 = shooter.Heat;
        var q0 = PerformanceSystem.GetMaxHeat(shooter);
        var q2 = GetQ2(shooter, q0);
        // A. 若 Q2 > Q1 > Q0，该机器人对应操作手电脑的第一视角可视度降低且发射机构锁定。 直到 Q1 = 0， 第一视角才会恢复正常，解锁发射机构
        // 若 Q1 > Q2 则全部时间发射机构锁定不再解锁
        if (q1 > q2)
        {
            BuffSystem.AddBuff(shooter.Id, RM2025ulBuffs.HeatGunLocked, 2, TimeSpan.MaxValue);
        }

        if (q1 > q0)
        {
            ModuleSystem.SetFpvVisibilityReduced(shooter.Id, true);
            BuffSystem.AddBuff(shooter.Id, RM2025ulBuffs.HeatGunLocked, 1, TimeSpan.MaxValue);
        }

        // if (Mathf.Approximately(q1, 0))
        if (MathF.Abs(q1) < 0.001f)
        {
            ModuleSystem.SetFpvVisibilityReduced(shooter.Id, false);
            if (
                !BuffSystem.TryGetBuff(shooter.Id, RM2025ulBuffs.HeatGunLocked, out float v)
                // || Mathf.Approximately(v, 1)
                || MathF.Abs(v - 1) < 0.001f
            )
            {
                BuffSystem.RemoveBuff(shooter.Id, RM2025ulBuffs.HeatGunLocked);
            }
        }

        SetHeat(shooter, q1 - cooldownDelta);

        return Task.CompletedTask;
    }

    private static float GetQ2(IShooter shooter, float q0)
    {
        return shooter.AmmoType switch
        {
            PerformanceSystemBase.AmmoType17mm => q0 + 100f,
            PerformanceSystemBase.AmmoType42mm => q0 + 200f,
        };
    }

    public void OnArmorHit(
        in Identity attacker,
        in Identity victimId,
        byte ammoType,
        byte armorType,
        byte armorId
    )
    {
        if (!EntitySystem.TryGetEntity(victimId, out IHealthed victim))
            return;

        var buffDamageFactor = 1f;

        if (BuffSystem.TryGetBuff(victimId, Buffs.DefenceBuff, out float defenceBuff))
        {
            if (defenceBuff >= 1)
                return;

            buffDamageFactor *= 1 - defenceBuff;
        }

        if (BuffSystem.TryGetBuff(attacker, Buffs.AttackBuff, out float attackBuff))
        {
            buffDamageFactor *= attackBuff;
        }

        if (buffDamageFactor <= 0)
            return;

        uint damage = 0;
        switch (ammoType)
        {
            case PerformanceSystemBase.AmmoType42mm:
            {
                if (victim is IRobot)
                {
                    // 42mm 对机器人伤害
                    damage = 100;
                }

                break;
            }
            case PerformanceSystemBase.AmmoType17mm:
            {
                if (victim is IRobot)
                {
                    // 17mm 对机器人伤害
                    damage = 10;
                }

                break;
            }
            case PerformanceSystemBase.AmmoTypeNone:
                damage = 2;
                break;
        }

        damage = (uint)(damage * buffDamageFactor);

        damage = LifeSystem.DecreaseHealth(victim, attacker, damage);

        if (EntitySystem.TryGetEntity(attacker, out IShooter shooter))
        {
            // OnDamage(shooter, victim, damage);
            Publisher.PublishAsync(
                new DamageCommand(shooter, victim, damage, ammoType, armorType, armorId)
            );
        }

        // damage sum
        if (attacker.Camp == Camp.Red)
        {
            SetRedDamageSum(GetRedDamageSum() + damage);
        }
        else if (attacker.Camp == Camp.Blue)
        {
            SetBlueDamageSum(GetBlueDamageSum() + damage);
        }
    }

    public void SetAmmoAllowance(IShooter shooter, int ammoAllowance)
    {
        IntCacheBox
            .WithWriterNamespace(shooter.Id)
            .Save(IShooter.AmmoAllowanceCacheKey, ammoAllowance);
    }

    /// <summary>
    /// 更新热量数据
    /// </summary>
    /// <param name="shooter"></param>
    /// <param name="q1"></param>
    private void SetHeat(IShooter shooter, float q1)
    {
        q1 = Math.Clamp(q1, 0, float.MaxValue);
        var q0 = PerformanceSystem.GetMaxHeat(shooter);

        // C. 若 Q1 ≥ 2Q0，立刻扣除血量 = (Q1 - 2Q0) / 250 * 上限血量。扣血后令 Q1 = 2Q0。
        if (q1 >= 2 * q0 && shooter is IHealthed healthed)
        {
            var maxHealth = PerformanceSystem.GetMaxHealth(healthed);
            var damage = (uint)((q1 - 2 * q0) / 250 * maxHealth);
            LifeSystem.DecreaseHealth(healthed, Identity.Server, damage);
            q1 = 2 * q0;
        }

        FloatCacheBox.WithWriterNamespace(shooter.Id).Save(IShooter.HeatCacheKey, q1);
    }

    public uint GetRedDamageSum()
    {
        return UintCacheBox.Load(RedDamageSumCacheKey);
    }

    public uint GetBlueDamageSum()
    {
        return UintCacheBox.Load(BlueDamageSumCacheKey);
    }

    public void SetRedDamageSum(uint damage)
    {
        UintCacheBox.Save(RedDamageSumCacheKey, damage);
    }

    public void SetBlueDamageSum(uint damage)
    {
        UintCacheBox.Save(BlueDamageSumCacheKey, damage);
    }
}