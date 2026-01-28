using System.Threading.Tasks;
using RoboSouls.JudgeSystem.Entities;
using RoboSouls.JudgeSystem.Events;
using RoboSouls.JudgeSystem.RoboMaster2026UL.Entities;
using RoboSouls.JudgeSystem.Systems;
using VContainer;
using VitalRouter;

namespace RoboSouls.JudgeSystem.RoboMaster2026UL.Systems;

[Routes]
public sealed partial class ModuleSystem : ModuleSystemBase
{
    [Inject] internal ICacheWriter<byte> ByteCacheWriter { get; set; }

    [Inject] internal LifeSystem LifeSystem { get; set; }

    [Inject] internal PerformanceSystemBase PerformanceSystemBase { get; set; }

    [Inject]
    internal void Inject(Router router)
    {
        MapTo(router);
    }

    public override bool TrySetRobotChassisType(IChassisd robotId, byte chassisType)
    {
        if (TimeSystem.Stage is JudgeSystemStage.Match)
            return false;

        if (robotId is Hero)
            if (
                chassisType
                is PerformanceSystemBase.ChassisTypePower
                or PerformanceSystemBase.ChassisTypeHealth
            )
            {
                SetRobotChassisTypeInternal(robotId, chassisType);
                return true;
            }

        if (robotId is Infantry)
            if (
                chassisType
                is PerformanceSystemBase.ChassisTypePower
                or PerformanceSystemBase.ChassisTypeHealth
            )
            {
                SetRobotChassisTypeInternal(robotId, chassisType);
                return true;
            }

        return false;
    }

    [Route]
    private void OnKill(KillEvent evt)
    {
        SetGunLocked(evt.Victim, true);
    }

    [Route]
    private void OnRevive(ReviveEvent evt)
    {
        if (evt.Reviver.IsSentry()) SetGunLocked(evt.Reviver, false);
    }

    public override bool IsGunLocked(in Identity id)
    {
        return base.IsGunLocked(in id)
               || BuffSystem.TryGetBuff(id, RM2026ulBuffs.HeatGunLocked, out Buff _);
    }

    public override bool TrySetRobotGunType(IShooter robotId, byte gunType)
    {
        if (TimeSystem.Stage is JudgeSystemStage.Match)
            return false;

        if (robotId is Hero)
            if (gunType == PerformanceSystemBase.GunType42mmDefault)
            {
                SetRobotGunTypeInternal(robotId, gunType);
                return true;
            }

        if (robotId is Infantry)
            if (
                gunType
                is PerformanceSystemBase.GunType17mmBurst
                or PerformanceSystemBase.GunType17mmCooldown
            )
            {
                SetRobotGunTypeInternal(robotId, gunType);
                return true;
            }

        return false;
    }

    public override bool IsSettingIncomplete(in Identity id)
    {
        if (!base.IsSettingIncomplete(id))
            return false;
        if (!EntitySystem.TryGetEntity(id, out IRobot r))
            return false;

        if (r is Hero h) return h.ChassisType == 0 || h.GunType == 0;

        if (r is Infantry i) return i.ChassisType == 0 || i.GunType == 0;

        return false;
    }

    private void SetRobotChassisTypeInternal(IChassisd robotId, byte chassisType)
    {
        ByteCacheWriter
            .WithWriterNamespace(robotId.Id)
            .Save(IChassisd.ChassisTypeCacheKey, chassisType);
        if (robotId is IHealthed healthed) LifeSystem.ResetHealth(healthed);
    }

    private void SetRobotGunTypeInternal(IShooter robotId, byte gunType)
    {
        ByteCacheWriter.WithWriterNamespace(robotId.Id).Save(IShooter.GunTypeCacheKey, gunType);
    }

    [Route]
    private Task OnJudgeSystemStageChange(JudgeSystemStageChangedEvent evt)
    {
        if (evt.Next != JudgeSystemStage.Countdown) return Task.CompletedTask;

        return Task.WhenAll(
            PerformanceFallbackForHero(Identity.RedHero),
            PerformanceFallbackForHero(Identity.BlueHero),
            PerformanceFallbackForInfantry(Identity.RedInfantry1),
            PerformanceFallbackForInfantry(Identity.BlueInfantry1)
        );
    }

    /// <summary>
    ///     性能自动设置
    ///     若不选择底盘或发射机构类型，则在五分钟比赛阶段开始后，未选择的底盘性能类型将被默认
    ///     选择为“血量优先”，未选择的枪管类型将被默认选择为“冷却优先”。
    /// </summary>
    private Task PerformanceFallbackForInfantry(Identity id)
    {
        if (!EntitySystem.TryGetEntity(id, out Infantry i))
            return Task.CompletedTask;

        if (i.ChassisType == 0) SetRobotChassisTypeInternal(i, PerformanceSystemBase.ChassisTypeHealth);

        if (i.GunType == 0) SetRobotGunTypeInternal(i, PerformanceSystemBase.GunType17mmCooldown);

        return Task.CompletedTask;
    }

    /// <summary>
    ///     性能自动设置
    ///     若不选择底盘或发射机构类型，则在五分钟比赛阶段开始后，未选择的底盘性能类型将被默认
    ///     选择为“血量优先”，未选择的枪管类型将被默认选择为“冷却优先”。
    /// </summary>
    private Task PerformanceFallbackForHero(Identity id)
    {
        if (!EntitySystem.TryGetEntity(id, out Hero h))
            return Task.CompletedTask;

        if (h.ChassisType == 0) SetRobotChassisTypeInternal(h, PerformanceSystemBase.ChassisTypeHealth);

        if (h.GunType == 0) SetRobotGunTypeInternal(h, PerformanceSystemBase.GunType42mmDefault);

        return Task.CompletedTask;
    }
}