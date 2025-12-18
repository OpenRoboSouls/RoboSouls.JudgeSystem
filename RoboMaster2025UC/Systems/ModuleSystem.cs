using System.Threading.Tasks;
using RoboSouls.JudgeSystem.Entities;
using RoboSouls.JudgeSystem.Events;
using RoboSouls.JudgeSystem.RoboMaster2025UC.Entities;
using RoboSouls.JudgeSystem.RoboMaster2025UC.Events;
using RoboSouls.JudgeSystem.Systems;
using VContainer;
using VitalRouter;

namespace RoboSouls.JudgeSystem.RoboMaster2025UC.Systems;

[Routes]
public sealed partial class ModuleSystem : ModuleSystemBase
{
    [Inject]
    internal void Inject(Router router)
    {
        MapTo(router);
    }

    [Inject]
    internal ICacheWriter<byte> ByteCacheWriter { get; set; }

    [Inject]
    internal LifeSystem LifeSystem { get; set; }

    public override bool TrySetRobotChassisType(IChassisd robotId, byte chassisType)
    {
        if (TimeSystem.Stage is JudgeSystemStage.Match)
            return false;

        if (robotId is Hero)
        {
            if (
                chassisType
                is PerformanceSystemBase.ChassisTypePower
                or PerformanceSystemBase.ChassisTypeHealth
            )
            {
                SetRobotChassisTypeInternal(robotId, chassisType);
                return true;
            }
        }

        if (robotId is Infantry)
        {
            if (
                chassisType
                is PerformanceSystemBase.ChassisTypePower
                or PerformanceSystemBase.ChassisTypeHealth
            )
            {
                SetRobotChassisTypeInternal(robotId, chassisType);
                return true;
            }
        }

        return false;
    }

    public override bool TrySetRobotGunType(IShooter robotId, byte gunType)
    {
        if (TimeSystem.Stage is JudgeSystemStage.Match)
            return false;

        if (robotId is Hero)
        {
            if (gunType == PerformanceSystemBase.GunType42mmDefault)
            {
                SetRobotGunTypeInternal(robotId, gunType);
                return true;
            }
        }

        if (robotId is Infantry)
        {
            if (
                gunType
                is PerformanceSystemBase.GunType17mmBurst
                or PerformanceSystemBase.GunType17mmCooldown
            )
            {
                SetRobotGunTypeInternal(robotId, gunType);
                return true;
            }
        }

        return false;
    }

    public override bool IsSettingIncomplete(in Identity id)
    {
        if (!base.IsSettingIncomplete(id))
            return false;

        if (!EntitySystem.TryGetEntity(id, out IRobot r))
            return false;

        if (r is Hero h)
        {
            return h.ChassisType == 0 || h.GunType == 0;
        }

        if (r is Infantry i)
        {
            return i.ChassisType == 0 || i.GunType == 0;
        }

        return false;
    }

    private void SetRobotChassisTypeInternal(IChassisd robotId, byte chassisType)
    {
        ByteCacheWriter
            .WithWriterNamespace(robotId.Id)
            .Save(IChassisd.ChassisTypeCacheKey, chassisType);
        if (robotId is IHealthed healthed)
        {
            LifeSystem.ResetHealth(healthed);
        }
    }

    private void SetRobotGunTypeInternal(IShooter robotId, byte gunType)
    {
        ByteCacheWriter.WithWriterNamespace(robotId.Id).Save(IShooter.GunTypeCacheKey, gunType);
    }

    [Route]
    private Task OnJudgeSystemStageChange(JudgeSystemStageChangedEvent evt)
    {
        if (evt.Next != JudgeSystemStage.Countdown)
        {
            return Task.CompletedTask;
        }

        return Task.WhenAll(
            PerformanceFallbackForHero(Identity.RedHero),
            PerformanceFallbackForHero(Identity.BlueHero),
            PerformanceFallbackForInfantry(Identity.RedInfantry1),
            PerformanceFallbackForInfantry(Identity.RedInfantry2),
            PerformanceFallbackForInfantry(Identity.BlueInfantry1),
            PerformanceFallbackForInfantry(Identity.BlueInfantry2)
        );
    }

    public override bool IsGunLocked(in Identity id)
    {
        return base.IsGunLocked(in id)
               || BuffSystem.TryGetBuff(id, RM2025ucBuffs.HeatGunLocked, out Buff _);
    }

    [Route]
    private void OnKill(KillEvent evt)
    {
        SetGunLocked(evt.Victim, true);
    }

    [Route]
    private void OnBuyRevive(BuyReviveEvent evt)
    {
        SetGunLocked(evt.Id, false);
    }

    /// <summary>
    /// 性能自动设置
    /// 若不选择底盘或发射机构类型，则在五分钟比赛阶段开始后，未选择的底盘性能类型将被默认
    /// 选择为“血量优先”，未选择的枪管类型将被默认选择为“冷却优先”。
    /// </summary>
    private Task PerformanceFallbackForInfantry(in Identity id)
    {
        if (!EntitySystem.TryGetEntity(id, out Infantry i))
            return Task.CompletedTask;

        if (i.ChassisType == 0)
        {
            SetRobotChassisTypeInternal(i, PerformanceSystemBase.ChassisTypeHealth);
        }

        if (i.GunType == 0)
        {
            SetRobotGunTypeInternal(i, PerformanceSystemBase.GunType17mmCooldown);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// 性能自动设置
    /// 若不选择底盘或发射机构类型，则在五分钟比赛阶段开始后，未选择的底盘性能类型将被默认
    /// 选择为“血量优先”，未选择的枪管类型将被默认选择为“冷却优先”。
    /// </summary>
    private Task PerformanceFallbackForHero(in Identity id)
    {
        if (!EntitySystem.TryGetEntity(id, out Hero h))
            return Task.CompletedTask;

        if (h.ChassisType == 0)
        {
            SetRobotChassisTypeInternal(h, PerformanceSystemBase.ChassisTypeHealth);
        }

        if (h.GunType == 0)
        {
            SetRobotGunTypeInternal(h, PerformanceSystemBase.GunType42mmDefault);
        }

        return Task.CompletedTask;
    }

    public override bool IsFpvLocked(in Identity id)
    {
        if (base.IsFpvLocked(id))
            return true;

        if (TimeSystem.Stage != JudgeSystemStage.Match)
            return false;

        if (BuffSystem.TryGetBuff(id, RM2025ucBuffs.DartHitBuff, out Buff _))
        {
            return true;
        }

        if (id.IsAerial())
        {
            if (
                EntitySystem.TryGetOperatedEntity(id, out Aerial aerial) && aerial.IsAirStriking
            )
            {
                return false;
            }

            return true;
        }

        return false;
    }

    public override float GetChassisPowerMultiplier(in Identity id)
    {
        if (
            id.IsHero()
            && BuffSystem.TryGetBuff(id, RM2025ucBuffs.HeroDeploymentModeBuff, out Buff _)
        )
        {
            return 0f;
        }

        return base.GetChassisPowerMultiplier(in id);
    }
}