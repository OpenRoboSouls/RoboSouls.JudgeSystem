using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RoboSouls.JudgeSystem.Entities;
using RoboSouls.JudgeSystem.Events;
using RoboSouls.JudgeSystem.RoboMaster2025UC.Entities;
using RoboSouls.JudgeSystem.RoboMaster2025UC.Events;
using RoboSouls.JudgeSystem.Systems;
using VContainer;
using VitalRouter;

namespace RoboSouls.JudgeSystem.RoboMaster2025UC.Systems;

/// <summary>
///     射击、伤害、热量系统
/// </summary>
[Routes]
public sealed partial class BattleSystem : IBattleSystem
{
    private static readonly int RedDamageSumCacheKey = "RedDamageSum".GetHashCode();
    private static readonly int BlueDamageSumCacheKey = "BlueDamageSum".GetHashCode();

    [Inject] internal ICommandPublisher Publisher { get; set; }

    [Inject] internal ICacheProvider<int> AmmoBox { get; set; }

    [Inject] internal LifeSystem LifeSystem { get; set; }

    [Inject] internal ITimeSystem TimeSystem { get; set; }

    [Inject] internal RM2025ucPerformanceSystem PerformanceSystem { get; set; }

    [Inject] internal EntitySystem EntitySystem { get; set; }

    [Inject] internal ILogger Logger { get; set; }

    [Inject] internal BuffSystem BuffSystem { get; set; }

    [Inject] internal BaseSystem BaseSystem { get; set; }

    [Inject] internal ICacheWriter<int> IntCacheBox { get; set; }

    [Inject] internal ICacheWriter<float> FloatCacheBox { get; set; }

    [Inject] internal ICacheProvider<uint> UintCacheBox { get; set; }

    [Inject] internal HeroSystem HeroSystem { get; set; }

    [Inject] internal ModuleSystem ModuleSystem { get; set; }

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

        // OnShoot(shooter, count);
        Publisher.PublishAsync(new ShootCommand(shooter, count));

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

        var buffDamageFactor = 1f;

        if (BuffSystem.TryGetBuff(victimId, Buffs.DefenceBuff, out float defenceBuff))
        {
            if (defenceBuff >= 1)
                return;

            buffDamageFactor *= 1 - defenceBuff;
        }

        if (BuffSystem.TryGetBuff(attacker, Buffs.AttackBuff, out float attackBuff)) buffDamageFactor *= attackBuff;

        if (BuffSystem.TryGetBuff(victimId, RM2025ucBuffs.Vulnerable, out float vulnerable))
            buffDamageFactor *= 1 + vulnerable;

        if (buffDamageFactor <= 0)
            return;

        uint damage = 0;
        switch (ammoType)
        {
            case PerformanceSystemBase.AmmoType42mm when victim is Base b:
                damage = armorId switch
                {
                    // 42mm => 基地大装甲模块（顶部）
                    0 => 200,

                    // 42mm => 基地大装甲模块（底部）
                    1 => 200,

                    // 42mm => 基地飞镖检测模块
                    2 => 200,
                    _ => throw new ArgumentOutOfRangeException()
                };
                if (
                    HeroSystem.IsDeploymentMode(attacker)
                    && HeroSystem.CanDeploymentHit(attacker)
                )
                {
                    damage = 300;
                    HeroSystem.OnDeploymentHit(attacker);
                }

                if (b.Health <= 2000 && armorId is 0 or 2)
                {
                    if (
                        HeroSystem.IsDeploymentMode(attacker)
                        && HeroSystem.CanDeploymentHit(attacker)
                    )
                        damage = 60;
                    else
                        damage = 40;
                }

                break;
            case PerformanceSystemBase.AmmoType42mm when victim is IRobot:
                // 42mm 对机器人伤害
                damage = 100;
                break;
            case PerformanceSystemBase.AmmoType42mm:
            {
                if (victim is Outpost) damage = 200;

                break;
            }
            case PerformanceSystemBase.AmmoType17mm when victim is Base:
                // 17mm 对基地伤害
                damage = armorId switch
                {
                    // 17mm => 基地大装甲模块（顶部）
                    0 => 1,

                    // 17mm => 基地大装甲模块（底部）
                    1 => 5,

                    // 17mm => 基地飞镖检测模块
                    2 => 0,

                    _ => throw new ArgumentOutOfRangeException()
                };
                break;
            case PerformanceSystemBase.AmmoType17mm when victim is IRobot:
                // 17mm 对机器人伤害
                damage = 10;
                break;
            case PerformanceSystemBase.AmmoType17mm:
            {
                if (victim is Outpost o)
                {
                    // 17mm => 前哨站中部装甲模块
                    if (armorId == 0)
                    {
                        // 旋转中
                        if (o.RotateSpeed > 0)
                            damage = 10;
                        // 静止中
                        else
                            damage = 5;
                    }
                    // 17mm => 前哨站飞镖检测模块
                    else if (armorId == 1)
                    {
                        damage = 0;
                    }
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
            Publisher.PublishAsync(
                new DamageCommand(shooter, victim, damage, ammoType, armorType, armorId)
            );

        // damage sum
        AddDamageSum(attacker.Camp, damage);
    }

    [Inject]
    internal void Inject(Router router)
    {
        MapTo(router);
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
        if (shooter is IHealthed h)
            if (h.IsDead())
            {
                SetHeat(shooter, 0);
                return Task.CompletedTask;
            }

        var cooldown = PerformanceSystem.GetCooldown(shooter);
        var cooldownDelta = (float)cooldown / 10;
        if (BuffSystem.TryGetBuff(shooter.Id, Buffs.CoolDownBuff, out float cooldownBuff))
        {
            if (
                BuffSystem.TryGetBuff(
                    shooter.Id,
                    Buffs.CoolDownAmountBuff,
                    out float cooldownAmount
                )
                && cooldownDelta + cooldownAmount > cooldownDelta * cooldownBuff
            )
                cooldownDelta += cooldownAmount;
            else
                cooldownDelta *= cooldownBuff;
        }

        var q1 = shooter.Heat;
        var q0 = PerformanceSystem.GetMaxHeat(shooter);
        var q2 = GetQ2(shooter, q0);
        // A. 若 Q2 > Q1 > Q0，该机器人对应操作手电脑的第一视角可视度降低且发射机构锁定。 直到 Q1 = 0， 第一视角才会恢复正常，解锁发射机构
        // 若 Q1 > Q2 则全部时间发射机构锁定不再解锁
        if (q1 > q2) BuffSystem.AddBuff(shooter.Id, RM2025ucBuffs.HeatGunLocked, 2, TimeSpan.MaxValue);

        if (q1 > q0)
        {
            ModuleSystem.SetFpvVisibilityReduced(shooter.Id, true);
            BuffSystem.AddBuff(shooter.Id, RM2025ucBuffs.HeatGunLocked, 1, TimeSpan.MaxValue);
        }

        // if (MathF.Approximately(q1, 0))
        if (MathF.Abs(q1 - 0) < 0.001f)
        {
            ModuleSystem.SetFpvVisibilityReduced(shooter.Id, false);
            if (
                !BuffSystem.TryGetBuff(shooter.Id, RM2025ucBuffs.HeatGunLocked, out float v)
                // || MathF.Approximately(v, 1)
                || MathF.Abs(v - 1) < 0.001f
            )
                BuffSystem.RemoveBuff(shooter.Id, RM2025ucBuffs.HeatGunLocked);
        }

        SetHeat(shooter, q1 - cooldownDelta);

        return Task.CompletedTask;
    }

    [Route]
    private void OnDartHit(DartHitEvent evt)
    {
        var dartStationId = evt.Camp switch
        {
            Camp.Red => DartSystem.RedDartStationId,
            Camp.Blue => DartSystem.BlueDartStationId,
            _ => throw new ArgumentOutOfRangeException()
        };

        if (evt.Target == DartTarget.Outpost)
        {
            var outpostId = evt.Camp switch
            {
                Camp.Red => Identity.BlueOutpost,
                Camp.Blue => Identity.RedOutpost,
                _ => throw new ArgumentOutOfRangeException()
            };

            var outpost = EntitySystem.Entities[outpostId] as Outpost;
            var defenceBuff = BuffSystem.TryGetBuff(
                outpostId,
                Buffs.DefenceBuff,
                out float buff
            )
                ? buff
                : 0;

            var buffDamageFactor = 1 - defenceBuff;
            if (buffDamageFactor <= 0)
                return;

            uint damage = 750;

            damage = (uint)(damage * buffDamageFactor);

            damage = LifeSystem.DecreaseHealth(outpost, dartStationId, damage);

            AddDamageSum(outpostId.Camp, damage);
        }
        else
        {
            var baseId = evt.Camp switch
            {
                Camp.Red => Identity.BlueBase,
                Camp.Blue => Identity.RedBase,
                _ => throw new ArgumentOutOfRangeException()
            };

            var b = EntitySystem.Entities[baseId] as Base;
            var defenceBuff = BuffSystem.TryGetBuff(baseId, Buffs.DefenceBuff, out float buff)
                ? buff
                : 0;

            var buffDamageFactor = 1 - defenceBuff;
            if (buffDamageFactor <= 0)
                return;

            uint damage = evt.Target switch
            {
                DartTarget.Fixed when b.Health > 2000 => 625,
                DartTarget.Fixed => 125,
                DartTarget.RandomFixed when b.Health > 2000 => 625,
                DartTarget.RandomFixed => 125,
                DartTarget.RandomMoving => 1200,
                _ => throw new ArgumentOutOfRangeException()
            };

            damage = (uint)(damage * buffDamageFactor);

            damage = LifeSystem.DecreaseHealth(b, dartStationId, damage);

            AddDamageSum(baseId.Camp, damage);
        }
    }

    [Route]
    private void OnKill(KillEvent evt)
    {
        if (!EntitySystem.TryGetOperatedEntity(evt.Victim, out IShooter shooter))
            return;

        SetHeat(shooter, 0);
    }

    private static float GetQ2(IShooter shooter, float q0)
    {
        return shooter.AmmoType switch
        {
            PerformanceSystemBase.AmmoType17mm => q0 + 100f,
            PerformanceSystemBase.AmmoType42mm => q0 + 200f
        };
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

    public uint GetDamageSum(Camp camp)
    {
        return UintCacheBox.Load(
            camp == Camp.Red ? RedDamageSumCacheKey : BlueDamageSumCacheKey
        );
    }

    internal void AddDamageSum(Camp camp, uint damage)
    {
        UintCacheBox.Save(
            camp == Camp.Red ? RedDamageSumCacheKey : BlueDamageSumCacheKey,
            GetDamageSum(camp) + damage
        );
    }
}