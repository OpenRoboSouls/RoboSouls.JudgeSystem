using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RoboSouls.JudgeSystem.Entities;
using RoboSouls.JudgeSystem.Events;
using RoboSouls.JudgeSystem.RoboMaster2025UL.Events;
using RoboSouls.JudgeSystem.Systems;
using VContainer;
using VitalRouter;

namespace RoboSouls.JudgeSystem.RoboMaster2025UL.Systems;

/// <summary>
/// 结算系统
/// 1. 一方的基地被击毁时， 当局比赛立即结束， 基地存活的一方获胜。
/// 2. 一局比赛时间耗尽时，双方基地均未被击毁，基地剩余血量高的一方获胜。
/// 3. 一局比赛时间耗尽时， 双方基地剩余血量一致， 哨兵机器人剩余血量高的一方获胜。
/// 4. 一局比赛时间耗尽时， 双方基地剩余血量一致且哨兵机器人剩余血量一致，全队攻击伤害高的一方获
/// 胜。
/// 5. 一局比赛时间耗尽时，双方基地剩余血量一致且哨兵机器人剩余血量一致、全队攻击伤害一致，则全
/// 队机器人总剩余血量高的一方获胜。
/// 6. 若上述条件无法判定胜利， 该局比赛视为平局。淘汰赛出现平局则立即加赛一局直至分出胜负。
/// </summary>
[Routes]
public sealed partial class SettlementSystem : ISystem
{
    [Inject]
    internal void Inject(Router router)
    {
        MapTo(router);
    }

    public const byte ReasonWinPoint = 1;
    public const byte ReasonTimeWinPoint = 2;
    public const byte ReasonTimeOutTeamDamage = 4;
    public const byte ReasonTimeOutTotalHp = 5;
    public const byte ReasonTimeOutDraw = 6;
    public const byte ReasonJudgeTerminate = 7;

    private bool _currentRoundSettled;

    [Inject]
    internal ITimeSystem TimeSystem { get; set; }

    [Inject]
    internal ICommandPublisher Publisher { get; set; }

    [Inject]
    internal EntitySystem EntitySystem { get; set; }

    [Inject]
    internal BattleSystem BattleSystem { get; set; }

    [Inject]
    internal WinPointSystem WinPointSystem { get; set; }

    public Task Reset(CancellationToken cancellation = new CancellationToken())
    {
        _currentRoundSettled = false;

        return Task.CompletedTask;
    }

    private void OnMatchSettle(Camp winner, byte reason)
    {
        if (_currentRoundSettled)
        {
            return;
        }

        _currentRoundSettled = true;

        TimeSystem.SetStage(JudgeSystemStage.Settlement);
        Publisher.PublishAsync(new MatchSettleEvent(winner, reason));
    }

    [Route]
    private void OnWinPoint(WinPointCommand evt)
    {
        if (TimeSystem.Stage != JudgeSystemStage.Match)
            return;
        if (_currentRoundSettled)
            return;

        if (evt.NewWinPoint >= RM2025ulPerformanceSystem.RequiredWinPoint)
        {
            OnMatchSettle(evt.Camp, ReasonWinPoint);
        }
    }

    /// <summary>
    /// 时间耗尽结算
    /// </summary>
    /// <param name="evt"></param>
    [Route]
    private void OnJudgeStageChange(JudgeSystemStageChangedEvent evt)
    {
        if (evt is not { Prev: JudgeSystemStage.Match, Next: JudgeSystemStage.Settlement })
            return;
        if (_currentRoundSettled)
            return;

        // 1. 一局比赛时间耗尽时，胜利点高的一方获胜。
        var redWinPoint = WinPointSystem.GetWinPoint(Camp.Red);
        var blueWinPoint = WinPointSystem.GetWinPoint(Camp.Blue);
        if (redWinPoint != blueWinPoint)
        {
            OnMatchSettle(
                redWinPoint > blueWinPoint ? Camp.Red : Camp.Blue,
                ReasonTimeWinPoint
            );
            return;
        }

        // 2. 胜利点相同，全队攻击伤害高的一方获胜
        var redDamageSum = BattleSystem.GetRedDamageSum();
        var blueDamageSum = BattleSystem.GetBlueDamageSum();
        if (redDamageSum != blueDamageSum)
        {
            OnMatchSettle(
                redDamageSum > blueDamageSum ? Camp.Red : Camp.Blue,
                ReasonTimeOutTeamDamage
            );
            return;
        }

        // 3. 全队攻击伤害一致，则全队机器人总剩余血量高的一方获胜。
        var redTotalHp = EntitySystem
            .GetOperatedEntities<IHealthed>(Camp.Red)
            .Sum(e => e.Health);
        var blueTotalHp = EntitySystem
            .GetOperatedEntities<IHealthed>(Camp.Blue)
            .Sum(e => e.Health);
        if (redTotalHp != blueTotalHp)
        {
            OnMatchSettle(
                redTotalHp > blueTotalHp ? Camp.Red : Camp.Blue,
                ReasonTimeOutTotalHp
            );
            return;
        }

        // 4. 若上述条件无法判定胜利， 该局比赛视为平局。淘汰赛出现平局则立即加赛一局直至分出胜负。
        OnMatchSettle(Camp.Spectator, ReasonTimeOutDraw);
    }

    [Route]
    private void OnJudgeTerminate(JudgePenaltyEvent evt)
    {
        if (TimeSystem.Stage != JudgeSystemStage.Match)
            return;
        if (_currentRoundSettled)
            return;
        if (evt.PenaltyType != PenaltyType.Lose)
            return;

        OnMatchSettle(evt.TargetId.Camp.GetOppositeCamp(), ReasonJudgeTerminate);
    }
}