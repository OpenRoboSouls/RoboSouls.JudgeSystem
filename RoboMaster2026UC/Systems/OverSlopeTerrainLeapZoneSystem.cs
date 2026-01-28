using System;
using RoboSouls.JudgeSystem.Systems;
using VContainer;
using VitalRouter;

namespace RoboSouls.JudgeSystem.RoboMaster2026UC.Systems;

/// <summary>
///     地形跨越增益点（飞坡）
/// </summary>
[Routes]
public sealed partial class OverSlopeTerrainLeapZoneSystem(
    ITimeSystem timeSystem,
    ICacheProvider<double> doubleCacheBox,
    BuffSystem buffSystem) : TerrainLeapZoneSystem(timeSystem, doubleCacheBox, buffSystem)
{
    public static readonly Identity OverSlopeTerrainLeapTriggerZoneId = new(
        Camp.Judge,
        150
    );

    public static readonly Identity OverSlopeTerrainLeapActivationZoneId = new(
        Camp.Judge,
        160
    );

    public override Identity TriggerZoneId => OverSlopeTerrainLeapTriggerZoneId;
    public override Identity ActivationZoneId => OverSlopeTerrainLeapActivationZoneId;
    public override int MaxActivationTime => 10;
    public override int BuffDuration => 30;

    [Inject]
    internal void Inject(Router router)
    {
        MapTo(router);
    }

    protected override void OnActivationStart(in Identity operatorId)
    {
    }

    protected override void OnActivationSuccess(in Identity operatorId, double activationTime)
    {
        // 25%防御增益，持续时间为 20 秒
        //  缓冲能量增加至 250J（详见 “5.1.4 底盘功率超限” ）
        //      触发增益的机器人在比赛开始 2-3 分钟、 3-5 分钟、 5-7 分钟时分别可获得 2、 3、 5 倍射击热量冷却增
        //    益， 持续时间为 20 秒
        base.OnActivationSuccess(operatorId, activationTime);

        buffSystem.AddBuff(
            operatorId,
            RM2026ucBuffs.TerrainLeapOverSlopeBuff,
            1,
            TimeSpan.FromSeconds(BuffDuration)
        );

        var cooldownBuffValue = timeSystem.StageTimeElapsed switch
        {
            >= 120 and < 180 => 2,
            >= 180 and < 300 => 3,
            >= 300 and < 420 => 5,
            _ => 0
        };

        if (cooldownBuffValue > 0)
            buffSystem.AddBuff(
                operatorId,
                Buffs.CoolDownBuff,
                cooldownBuffValue,
                TimeSpan.FromSeconds(BuffDuration)
            );

        buffSystem.AddBuff(
            operatorId,
            Buffs.DefenceBuff,
            0.25f,
            TimeSpan.FromSeconds(BuffDuration)
        );
        buffSystem.AddBuff(operatorId, Buffs.PowerBuff, 1.5f, TimeSpan.FromSeconds(5));
    }
}