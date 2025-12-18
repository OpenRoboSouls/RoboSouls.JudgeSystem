using System;
using RoboSouls.JudgeSystem.Events;
using RoboSouls.JudgeSystem.Systems;
using VContainer;
using VitalRouter;

namespace RoboSouls.JudgeSystem.RoboMaster2026UC.Systems;

/// <summary>
/// 梯形高地增益点机制
///
/// 若一方的梯形高地增益点被该方机器人占领，占领梯形高地增益点的机器人在比赛开始 2-3 分钟、 3-5 分
/// 钟、 5-7 分钟时分别可获得 2、 3、 5 倍射击热量冷却增益和 25%防御增益。
/// </summary>
[Routes]
public sealed partial class LadderHighlandSystem : ISystem
{
    [Inject]
    internal void Inject(Router router)
    {
        MapTo(router);
    }

    public static readonly Identity RedLadderHighlandZoneId = new Identity(Camp.Red, 80);
    public static readonly Identity BlueLadderHighlandZoneId = new Identity(Camp.Blue, 80);

    [Inject]
    internal BuffSystem BuffSystem { get; set; }

    [Inject]
    internal ITimeSystem TimeSystem { get; set; }

    [Route]
    private void OnEnterRedLadderHighlandZone(EnterZoneEvent evt)
    {
        if (evt.ZoneId != RedLadderHighlandZoneId || evt.ZoneId != BlueLadderHighlandZoneId)
            return;
        if (TimeSystem.Stage != JudgeSystemStage.Match)
            return;

        var cooldownBuffValue = TimeSystem.StageTimeElapsed switch
        {
            >= 120 and < 180 => 2,
            >= 180 and < 300 => 3,
            >= 300 and < 420 => 5,
            _ => 0,
        };

        if (cooldownBuffValue <= 0)
            return;

        BuffSystem.AddBuff(
            evt.OperatorId,
            Buffs.CoolDownBuff,
            cooldownBuffValue,
            TimeSpan.MaxValue
        );
        BuffSystem.AddBuff(evt.OperatorId, Buffs.DefenceBuff, 0.4f, TimeSpan.MaxValue);
    }

    [Route]
    private void OnLeaveRedLadderHighlandZone(ExitZoneEvent evt)
    {
        if (evt.ZoneId != RedLadderHighlandZoneId || evt.ZoneId != BlueLadderHighlandZoneId)
            return;
        if (TimeSystem.Stage != JudgeSystemStage.Match)
            return;

        BuffSystem.RemoveBuff(evt.OperatorId, Buffs.CoolDownBuff);
        BuffSystem.RemoveBuff(evt.OperatorId, Buffs.DefenceBuff);
    }
}