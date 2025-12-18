using System;
using RoboSouls.JudgeSystem.Systems;
using VContainer;
using VitalRouter;

namespace RoboSouls.JudgeSystem.RoboMaster2026UC.Systems;

/// <summary>
/// 地形跨越增益点（公路）
/// </summary>
[Routes]
public sealed partial class RoadTerrainLeapZoneSystem : TerrainLeapZoneSystem
{
    [Inject]
    internal void Inject(Router router)
    {
        MapTo(router);
    }

    public static readonly Identity RoadTerrainLeapTriggerZoneId = new Identity(
        Camp.Judge,
        130
    );
    public static readonly Identity RoadTerrainLeapActivationZoneId = new Identity(
        Camp.Judge,
        140
    );

    public override Identity TriggerZoneId => RoadTerrainLeapTriggerZoneId;
    public override Identity ActivationZoneId => RoadTerrainLeapActivationZoneId;
    public override int MaxActivationTime => 3;
    public override int BuffDuration => 5;

    protected override void OnActivationStart(in Identity operatorId) { }

    protected override void OnActivationSuccess(in Identity operatorId, double activationTime)
    {
        // 25%防御增益，持续时间为 5 秒
        //  触发增益的机器人在比赛开始 2-3 分钟、 3-5 分钟、 5-7 分钟时分别可获得 2、 3、 5 倍射击热量冷却增
        //     益，持续时间为 5 秒
        //      同一机器人在获得地形跨越增益（公路）后的 15 秒内，不能重复获得地形跨越增益（公路）
        base.OnActivationSuccess(operatorId, activationTime);
        if (BuffSystem.TryGetBuff(operatorId, RM2026ucBuffs.TerrainLeapRoadBuff, out Buff _))
        {
            return;
        }

        BuffSystem.AddBuff(
            operatorId,
            RM2026ucBuffs.TerrainLeapRoadBuff,
            1,
            TimeSpan.FromSeconds(15)
        );

        var cooldownBuffValue = TimeSystem.StageTimeElapsed switch
        {
            >= 120 and < 180 => 2,
            >= 180 and < 300 => 3,
            >= 300 and < 420 => 5,
            _ => 0,
        };

        if (cooldownBuffValue > 0)
        {
            BuffSystem.AddBuff(
                operatorId,
                Buffs.CoolDownBuff,
                cooldownBuffValue,
                TimeSpan.FromSeconds(BuffDuration)
            );
        }

        BuffSystem.AddBuff(
            operatorId,
            Buffs.DefenceBuff,
            0.25f,
            TimeSpan.FromSeconds(BuffDuration)
        );
    }
}