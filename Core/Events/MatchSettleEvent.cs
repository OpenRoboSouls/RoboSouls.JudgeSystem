namespace RoboSouls.JudgeSystem.Events;

/// <summary>
///     比赛结束结算
/// </summary>
public readonly record struct MatchSettleEvent(Camp Winner, byte Reason) : IJudgeSystemEvent<MatchSettleEvent>;