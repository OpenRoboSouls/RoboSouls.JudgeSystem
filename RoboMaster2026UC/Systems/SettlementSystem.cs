using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RoboSouls.JudgeSystem.Entities;
using RoboSouls.JudgeSystem.Events;
using RoboSouls.JudgeSystem.RoboMaster2026UC.Entities;
using RoboSouls.JudgeSystem.Systems;
using VContainer;
using VitalRouter;

namespace RoboSouls.JudgeSystem.RoboMaster2026UC.Systems;

/// <summary>
/// 结算系统
/// 1. 一局比赛时间耗尽或一方基地被击毁时，基地剩余血量高的一方获胜。
/// 2. 一局比赛时间耗尽时，若双方基地剩余血量一致，前哨站剩余血量高的一方获胜。
/// 3. 一局比赛时间耗尽时，若双方基地剩余血量一致，前哨站血量一致或均被击毁， 全队攻击伤害高的一
/// 方获胜。
/// 4. 一局比赛时间耗尽时，若双方基地剩余血量一致， 前哨站血量一致或均被击毁， 全队攻击伤害一致，
/// 全队总剩余血量高的一方获胜
/// 5. 若上述条件无法判定胜负，该局比赛视为平局。在 BO3 和 BO5 的对局中，出现平局则立即加赛一局，
/// 直至分出胜负。
/// </summary>
[Routes]
public sealed partial class SettlementSystem(
    ITimeSystem timeSystem,
    ICommandPublisher publisher,
    EntitySystem entitySystem,
    BattleSystem battleSystem)
    : ISystem
{
    [Inject]
    internal void Inject(Router router)
    {
        MapTo(router);
    }

    public const byte ReasonBaseDestroyed = 1;
    public const byte ReasonTimeOutBaseHp = 2;
    public const byte ReasonTimeOutOutpostHp = 3;
    public const byte ReasonTimeOutTeamDamage = 4;
    public const byte ReasonTimeOutTotalHp = 5;
    public const byte ReasonTimeOutDraw = 6;
    public const byte ReasonJudgeTerminate = 7;

    private bool _currentRoundSettled;

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

        timeSystem.SetStage(JudgeSystemStage.Settlement);
        publisher.PublishAsync(new MatchSettleEvent(winner, reason));
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
        ;

        // 一局比赛时间耗尽时，双方基地均未被击毁，基地剩余血量高的一方获胜。
        var redBase = entitySystem.Entities[Identity.RedBase] as Base;
        var blueBase = entitySystem.Entities[Identity.BlueBase] as Base;

        if (!redBase.IsDead() && !blueBase.IsDead() && redBase.Health != blueBase.Health)
        {
            OnMatchSettle(
                redBase.Health > blueBase.Health ? Camp.Red : Camp.Blue,
                ReasonTimeOutBaseHp
            );
            return;
        }

        // 一局比赛时间耗尽时，若双方基地剩余血量一致，前哨站剩余血量高的一方获胜。
        var redOutpost = entitySystem.Entities[Identity.RedOutpost] as Outpost;
        var blueOutpost = entitySystem.Entities[Identity.BlueOutpost] as Outpost;

        if (!redBase.IsDead() && !blueBase.IsDead() && redOutpost.Health != blueOutpost.Health)
        {
            OnMatchSettle(
                redOutpost.Health > blueOutpost.Health ? Camp.Red : Camp.Blue,
                ReasonTimeOutOutpostHp
            );
            return;
        }

        // 一局比赛时间耗尽时，若双方基地剩余血量一致，前哨站血量一致或均被击毁， 全队攻击伤害高的一方获胜。
        var redDamageSum = battleSystem.GetDamageSum(Camp.Red);
        var blueDamageSum = battleSystem.GetDamageSum(Camp.Blue);

        if (redDamageSum != blueDamageSum)
        {
            OnMatchSettle(
                redDamageSum > blueDamageSum ? Camp.Red : Camp.Blue,
                ReasonTimeOutTeamDamage
            );
            return;
        }

        // 一局比赛时间耗尽时，若双方基地剩余血量一致， 前哨站血量一致或均被击毁， 全队攻击伤害一致，全队总剩余血量高的一方获胜
        var redTotalHp = entitySystem
            .GetOperatedEntities<IHealthed>(Camp.Red)
            .Sum(e => e.Health);
        var blueTotalHp = entitySystem
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

        OnMatchSettle(Camp.Spectator, ReasonTimeOutDraw);
    }

    [Route]
    private void OnJudgeTerminate(JudgePenaltyEvent evt)
    {
        if (timeSystem.Stage != JudgeSystemStage.Match)
            return;
        if (_currentRoundSettled)
            return;
        if (evt.PenaltyType != PenaltyType.Lose)
            return;

        OnMatchSettle(evt.TargetId.Camp.GetOppositeCamp(), ReasonJudgeTerminate);
    }

    /// <summary>
    /// 裁判终止比赛
    /// </summary>
    public void JudgeTerminate()
    {
        if (timeSystem.Stage != JudgeSystemStage.Match)
            return;
        if (_currentRoundSettled)
            return;

        OnMatchSettle(Camp.Judge, ReasonJudgeTerminate);
    }

    /// <summary>
    /// 基地击毁计算
    /// </summary>
    /// <param name="evt"></param>
    [Route]
    private void OnKill(KillEvent evt)
    {
        if (timeSystem.Stage != JudgeSystemStage.Match)
            return;
        if (_currentRoundSettled)
            return;
        if (evt.Victim == Identity.RedBase)
        {
            OnMatchSettle(Camp.Blue, ReasonBaseDestroyed);
        }
        else if (evt.Victim == Identity.BlueBase)
        {
            OnMatchSettle(Camp.Red, ReasonBaseDestroyed);
        }
    }
}