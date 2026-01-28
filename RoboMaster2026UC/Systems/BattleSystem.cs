using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RoboSouls.JudgeSystem.Attributes;
using RoboSouls.JudgeSystem.Entities;
using RoboSouls.JudgeSystem.Events;
using RoboSouls.JudgeSystem.RoboMaster2026UC.Entities;
using RoboSouls.JudgeSystem.RoboMaster2026UC.Events;
using RoboSouls.JudgeSystem.Systems;
using VContainer;
using VitalRouter;

namespace RoboSouls.JudgeSystem.RoboMaster2026UC.Systems;

/// <summary>
///     射击、伤害、热量系统
/// </summary>
[Routes]
public sealed partial class BattleSystem(
    ICommandPublisher publisher,
    LifeSystem lifeSystem,
    ITimeSystem timeSystem,
    RM2026ucPerformanceSystem performanceSystem,
    EntitySystem entitySystem,
    BuffSystem buffSystem,
    ICacheWriter<int> intCacheBox,
    ICacheWriter<float> floatCacheBox,
    ICacheProvider<uint> uintCacheBox,
    HeroSystem heroSystem,
    ModuleSystem moduleSystem)
    : IBattleSystem
{
    [Property(nameof(uintCacheBox), PropertyStorageMode.Camp)]
    public partial uint DamageSum { get; internal set; }

    public Task Reset(CancellationToken cancellation = new())
    {
        timeSystem.RegisterRepeatAction(0.1, CooldownTask);

        return Task.CompletedTask;
    }

    public bool TryShoot(IShooter shooter, int count)
    {
        if (shooter.AmmoAllowance <= -3) return false;

        // 允许一定超发
        SetAmmoAllowance(shooter, shooter.AmmoAllowance - count);
        SetHeat(shooter, shooter.Heat + performanceSystem.GetHeatDelta(shooter) * count);

        // OnShoot(shooter, count);
        publisher.PublishAsync(new ShootCommand(shooter, count));

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
        if (!entitySystem.TryGetEntity(victimId, out IHealthed victim))
            return;

        var buffDamageFactor = 1f;

        if (buffSystem.TryGetBuff(victimId, Buffs.DefenceBuff, out float defenceBuff))
        {
            if (defenceBuff >= 1)
                return;

            buffDamageFactor *= 1 - defenceBuff;
        }

        if (buffSystem.TryGetBuff(attacker, Buffs.AttackBuff, out float attackBuff)) buffDamageFactor *= attackBuff;

        if (buffSystem.TryGetBuff(victimId, RM2026ucBuffs.Vulnerable, out float vulnerable))
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
                    heroSystem.IsDeploymentMode(attacker)
                    && heroSystem.CanDeploymentHit(attacker)
                )
                {
                    if ((buffDamageFactor > 1f) & (buffDamageFactor < 1.5f)) buffDamageFactor = 1.5f;
                    heroSystem.OnDeploymentHit(attacker);
                }

                break;
            case PerformanceSystemBase.AmmoType42mm when victim is IRobot:
                // 42mm 对机器人伤害
                damage = 200;
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
                    0 => 20,

                    // 17mm => 基地大装甲模块（底部）
                    1 => 20,

                    // 17mm => 基地飞镖检测模块
                    2 => 0,

                    _ => throw new ArgumentOutOfRangeException()
                };
                break;
            case PerformanceSystemBase.AmmoType17mm when victim is IRobot:
                // 17mm 对机器人伤害
                damage = 20;
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
                            damage = 20;
                        // 静止中
                        else
                            damage = 20;
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

        damage = lifeSystem.DecreaseHealth(victim, attacker, damage);

        if (entitySystem.TryGetEntity(attacker, out IShooter shooter))
            publisher.PublishAsync(
                new DamageCommand(shooter, victim, damage, ammoType, armorType, armorId)
            );

        // damage sum
        SetDamageSum(attacker.Camp, GetDamageSum(attacker.Camp) + damage);
    }

    [Inject]
    internal void Inject(Router router)
    {
        MapTo(router);
    }

    private Task CooldownTask()
    {
        return Task.WhenAll(
            entitySystem
                .Entities.Values.OfType<IShooter>()
                .Where(entitySystem.HasOperator)
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

        var cooldown = performanceSystem.GetCooldown(shooter);
        var cooldownDelta = (float)cooldown / 10;
        if (buffSystem.TryGetBuff(shooter.Id, Buffs.CoolDownBuff, out float cooldownBuff))
        {
            if (
                buffSystem.TryGetBuff(
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
        var q0 = performanceSystem.GetMaxHeat(shooter);
        var q2 = GetQ2(shooter, q0);
        // A. 若 Q2 > Q1 > Q0，该机器人对应操作手电脑的第一视角可视度降低且发射机构锁定。 直到 Q1 = 0， 第一视角才会恢复正常，解锁发射机构
        // 若 Q1 > Q2 则全部时间发射机构锁定不再解锁
        if (q1 > q2) buffSystem.AddBuff(shooter.Id, RM2026ucBuffs.HeatGunLocked, 2, TimeSpan.MaxValue);

        if (q1 > q0)
        {
            moduleSystem.SetFpvVisibilityReduced(shooter.Id, true);
            buffSystem.AddBuff(shooter.Id, RM2026ucBuffs.HeatGunLocked, 1, TimeSpan.MaxValue);
        }

        // if (Mathf.Approximately(q1, 0))
        if (MathF.Abs(q1 - 0) < 0.001f)
        {
            moduleSystem.SetFpvVisibilityReduced(shooter.Id, false);
            if (
                !buffSystem.TryGetBuff(shooter.Id, RM2026ucBuffs.HeatGunLocked, out float v)
                // || Mathf.Approximately(v, 1)
                || MathF.Abs(v - 1) < 0.001f
            )
                buffSystem.RemoveBuff(shooter.Id, RM2026ucBuffs.HeatGunLocked);
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

            var outpost = entitySystem.Entities[outpostId] as Outpost;
            var defenceBuff = buffSystem.TryGetBuff(
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

            damage = lifeSystem.DecreaseHealth(outpost, dartStationId, damage);

            SetDamageSum(outpostId.Camp, GetDamageSum(outpostId.Camp) + damage);
        }
        else
        {
            var baseId = evt.Camp switch
            {
                Camp.Red => Identity.BlueBase,
                Camp.Blue => Identity.RedBase,
                _ => throw new ArgumentOutOfRangeException()
            };

            var b = entitySystem.Entities[baseId] as Base;
            var defenceBuff = buffSystem.TryGetBuff(baseId, Buffs.DefenceBuff, out float buff)
                ? buff
                : 0;

            var buffDamageFactor = 1 - defenceBuff;
            if (buffDamageFactor <= 0)
                return;

            uint damage = evt.Target switch
            {
                DartTarget.Fixed => 200,
                DartTarget.RandomFixed => 300,
                DartTarget.RandomMoving => 625,
                DartTarget.EndMoving => 1000,
                _ => throw new ArgumentOutOfRangeException()
            };

            damage = (uint)(damage * buffDamageFactor);

            damage = lifeSystem.DecreaseHealth(b, dartStationId, damage);

            SetDamageSum(baseId.Camp, GetDamageSum(baseId.Camp) + damage);
        }
    }

    [Route]
    private void OnKill(KillEvent evt)
    {
        if (!entitySystem.TryGetOperatedEntity(evt.Victim, out IShooter shooter))
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
        intCacheBox
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
        var q0 = performanceSystem.GetMaxHeat(shooter);

        floatCacheBox.WithWriterNamespace(shooter.Id).Save(IShooter.HeatCacheKey, q1);
    }
}