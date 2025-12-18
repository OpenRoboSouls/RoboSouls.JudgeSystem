using System;
using VContainer;
using VitalRouter;

namespace RoboSouls.JudgeSystem.RoboMaster2026UC.Systems;

/// <summary>
/// 地形跨越增益点（高地）
/// </summary>
[Routes]
public sealed partial class HighlandTerrainLeapZoneSystem : TerrainLeapZoneSystem
{
    [Inject]
    internal void Inject(Router router)
    {
        MapTo(router);
    }

    public static readonly Identity HighlandTerrainLeapTriggerZoneId = new Identity(
        Camp.Judge,
        170
    );
    public static readonly Identity HighlandTerrainLeapActivationZoneId = new Identity(
        Camp.Judge,
        180
    );

    public override Identity TriggerZoneId => HighlandTerrainLeapTriggerZoneId;
    public override Identity ActivationZoneId => HighlandTerrainLeapActivationZoneId;
    public override int MaxActivationTime => 5;
    public override int BuffDuration => 30;

    protected override void OnActivationStart(in Identity operatorId) { }

    protected override void OnActivationSuccess(in Identity operatorId, double activationTime)
    {
        //25%防御增益，持续时间为 20 秒
        // 触发增益的机器人在比赛开始 2-3 分钟、 3-5 分钟、 5-7 分钟时分别可获得 2、 3、 5 倍射击热量冷却增
        //    益，持续时间为 20 秒
        base.OnActivationSuccess(operatorId, activationTime);
        BuffSystem.AddBuff(
            operatorId,
            RM2026ucBuffs.TerrainLeapHighlandBuff,
            1,
            TimeSpan.FromSeconds(BuffDuration)
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
            0.5f,
            TimeSpan.FromSeconds(BuffDuration)
        );
    }
}