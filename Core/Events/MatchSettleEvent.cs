using System;

namespace RoboSouls.JudgeSystem.Events;

/// <summary>
/// 比赛结束结算
/// </summary>
public readonly struct MatchSettleEvent(Camp winner, byte reason) : IJudgeSystemEvent<MatchSettleEvent>,
    IEquatable<MatchSettleEvent>
{
    public readonly Camp Winner = winner;
    public readonly byte Reason = reason;

    public bool Equals(MatchSettleEvent other)
    {
        return Winner == other.Winner && Reason == other.Reason;
    }

    public override bool Equals(object obj)
    {
        return obj is MatchSettleEvent other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Winner, Reason);
    }
}