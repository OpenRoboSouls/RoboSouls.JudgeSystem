using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RoboSouls.JudgeSystem.Entities;
using RoboSouls.JudgeSystem.RoboMaster2024UL.Entities;
using RoboSouls.JudgeSystem.Systems;
using VContainer;
using VitalRouter;

namespace RoboSouls.JudgeSystem.RoboMaster2024UL.Systems;

/// <summary>
///     射击、伤害、热量系统
/// </summary>
public sealed class BattleSystem : IBattleSystem
{
    private static readonly int RedDamageSumCacheKey = "RedDamageSum".GetHashCode();
    private static readonly int BlueDamageSumCacheKey = "BlueDamageSum".GetHashCode();

    [Inject] internal ICommandPublisher Publisher { get; set; }

    [Inject] internal ICacheProvider<int> AmmoBox { get; set; }

    [Inject] internal LifeSystem LifeSystem { get; set; }

    [Inject] internal ITimeSystem TimeSystem { get; set; }

    [Inject] internal RM2024ulPerformanceSystem PerformanceSystem { get; set; }

    [Inject] internal EntitySystem EntitySystem { get; set; }

    [Inject] internal ILogger Logger { get; set; }

    [Inject] internal BuffSystem BuffSystem { get; set; }

    [Inject] internal BaseSystem BaseSystem { get; set; }

    [Inject] internal ICacheWriter<int> IntCacheBox { get; set; }

    [Inject] internal ICacheWriter<float> FloatCacheBox { get; set; }

    [Inject] internal ICacheProvider<uint> UintCacheBox { get; set; }

    [Inject] internal ModuleSystem ModuleSystem { get; set; }

    [Inject] internal CentralZoneSystem CentralZoneSystem { get; set; }

    internal Action<IShooter, IHealthed, uint> OnDamage { get; set; } = delegate { };

    // use delegate instead of event to avoid redundant routing
    internal Action<IShooter, int> OnShoot { get; set; } = delegate { };

    public Task Reset(CancellationToken cancellation = new())
    {
        TimeSystem.RegisterRepeatAction(0.1, CooldownTask);

        return Task.CompletedTask;
    }

    public bool TryShoot(IShooter shooter, int count)
    {
        if (shooter.AmmoAllowance <= -3) return false;

        // 允许一定超发
        SetAmmoAllowance(shooter, shooter.AmmoAllowance - count);
        SetHeat(shooter, shooter.Heat + PerformanceSystem.GetHeatDelta(shooter) * count);

        OnShoot(shooter, count);

        return true;
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

        if (!BuffSystem.TryGetBuff(victimId, Buffs.DefenceBuff, out float defenceBuff))
            defenceBuff = 0;
        if (!BuffSystem.TryGetBuff(attacker, Buffs.AttackBuff, out float attackBuff))
            attackBuff = 0;

        // 无敌
        if (Math.Abs(defenceBuff - 1) < 0.01)
            return;

        var buffDamageFactor = 1 + (attackBuff - defenceBuff);
        if (buffDamageFactor <= 0)
            return;

        uint damage = 0;
        switch (ammoType)
        {
            case PerformanceSystemBase.AmmoType42mm when victim is Base:
                // 42mm 对基地伤害
                damage = 200;
                break;
            case PerformanceSystemBase.AmmoType42mm:
            {
                if (victim is IRobot)
                    // 42mm 对机器人伤害
                    damage = 100;

                break;
            }
            case PerformanceSystemBase.AmmoType17mm when victim is Base:
                // 17mm 对基地伤害
                damage = 5;
                break;
            case PerformanceSystemBase.AmmoType17mm:
            {
                if (victim is IRobot)
                    // 17mm 对机器人伤害
                    damage = 10;

                break;
            }
            case PerformanceSystemBase.AmmoTypeNone:
                damage = 2;
                break;
        }

        damage = (uint)(damage * buffDamageFactor);

        // shoot base
        if (victim is Base { Shield: > 0 } b)
            damage = BaseSystem.DecreaseShield(b, damage);
        else
            damage = LifeSystem.DecreaseHealth(victim, attacker, damage);

        if (EntitySystem.TryGetEntity(attacker, out IShooter shooter))
            OnDamage(shooter, victim, damage);

        // damage sum
        if (attacker.Camp == Camp.Red)
            SetRedDamageSum(GetRedDamageSum() + damage);
        else if (attacker.Camp == Camp.Blue) SetBlueDamageSum(GetBlueDamageSum() + damage);

        CentralZoneSystem.OnCentralZoneHit(victimId, ammoType);
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
    ///     热量冷却结算 10Hz
    ///     枪口热量按 10Hz 的频率结算冷却，每个检测周期热量冷却值 = 每秒
    ///     冷却值 / 10。
    /// </summary>
    /// <param name="shooter"></param>
    private Task CooldownTaskFor(IShooter shooter)
    {
        var cooldown = PerformanceSystem.GetCooldown(shooter);
        var cooldownDelta = (float)cooldown / 10;

        var q1 = shooter.Heat;
        var q0 = PerformanceSystem.GetMaxHeat(shooter);
        // A. 若 Q1 > Q0，该机器人对应操作手电脑的第一视角可视度降低。 直到 Q1 ≤ Q0， 第一视角才会恢复正
        if (q1 > q0)
            ModuleSystem.SetFpvVisibilityReduced(shooter.Id, true);
        else
            ModuleSystem.SetFpvVisibilityReduced(shooter.Id, false);

        // B. 若 2Q0 > Q1 > Q0，每 100 ms 扣除血量 = ((Q1 - Q0) / 250) / 10 * 上限血量。扣血后结算冷却。
        if (q1 > q0 && q1 < 2 * q0 && shooter is IHealthed healthed)
        {
            var maxHealth = PerformanceSystem.GetMaxHealth(healthed);
            var damage = (uint)((q1 - q0) / 250 / 10 * maxHealth);
            LifeSystem.DecreaseHealth(healthed, Identity.Server, damage);
        }

        SetHeat(shooter, q1 - cooldownDelta);

        return Task.CompletedTask;
    }

    public void SetAmmoAllowance(IShooter shooter, int ammoAllowance)
    {
        IntCacheBox
            .WithWriterNamespace(shooter.Id)
            .Save(IShooter.AmmoAllowanceCacheKey, ammoAllowance);
    }

    /// <summary>
    ///     更新热量数据
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