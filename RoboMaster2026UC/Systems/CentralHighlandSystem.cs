using System;
using RoboSouls.JudgeSystem.RoboMaster2026UC.Entities;
using RoboSouls.JudgeSystem.Systems;
using VContainer;
using VitalRouter;

namespace RoboSouls.JudgeSystem.RoboMaster2026UC.Systems;

/// <summary>
///     中央高地增益点机制
///     英雄、步兵、哨兵机器人均可占领中央高地增益点。 不相连的中央高地增益点占领关系相互独立。 同一方
///     的多台机器人可同时占领中央高地增益点。 若一方机器人占领中央高地增益点，另一方机器人无法同时占
///     领。
///     占领中央高地增益点的机器人在比赛开始 2-3 分钟、 3-5 分钟、 5-7 分钟时分别可获得 2、 3、 5 倍射击热量
///     冷却增益。
/// </summary>
[Routes]
public sealed partial class CentralHighlandSystem(
    EntitySystem entitySystem,
    BuffSystem buffSystem,
    ITimeSystem timeSystem)
    : OccupyZoneSystemBase
{
    public static readonly Identity CentralHighlandZoneId = new(Camp.Judge, 120);

    public override Identity ZoneId => CentralHighlandZoneId;

    [Inject]
    internal void Inject(Router router)
    {
        MapTo(router);
    }

    protected override void OnZoneOccupied(Camp camp)
    {
    }

    protected override void OnZoneLost(Camp camp)
    {
    }

    protected override void OnOccupierEnterZone(in Identity operatorId)
    {
        if (timeSystem.Stage != JudgeSystemStage.Match)
            return;

        if (entitySystem.Entities[operatorId] is not (Hero or Infantry or Sentry))
            return;
        var cooldownBuffValue = timeSystem.StageTimeElapsed switch
        {
            >= 120 and < 180 => 2,
            >= 180 and < 300 => 3,
            >= 300 and < 420 => 5,
            _ => 0
        };

        if (cooldownBuffValue <= 0)
            return;

        buffSystem.AddBuff(
            operatorId,
            Buffs.CoolDownBuff,
            cooldownBuffValue,
            TimeSpan.MaxValue
        );
    }

    protected override void OnOccupierLeaveZone(in Identity operatorId)
    {
        if (timeSystem.Stage != JudgeSystemStage.Match)
            return;

        if (entitySystem.Entities[operatorId] is not (Hero or Infantry or Sentry))
            return;
        buffSystem.RemoveBuff(operatorId, Buffs.CoolDownBuff);
    }
}