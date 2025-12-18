using System;
using RoboSouls.JudgeSystem.Systems;

namespace RoboSouls.JudgeSystem.Events;

public readonly struct JudgePenaltyEvent(
    PenaltyType penaltyType,
    Identity targetId,
    Identity judgeId,
    byte reason)
    : IJudgeSystemEvent<JudgePenaltyEvent>,
        IEquatable<JudgePenaltyEvent>
{
    public readonly PenaltyType PenaltyType = penaltyType;
    public readonly Identity TargetId = targetId;
    public readonly Identity JudgeId = judgeId;
    public readonly byte Reason = reason;

    public bool Equals(JudgePenaltyEvent other)
    {
        return PenaltyType == other.PenaltyType
               && TargetId.Equals(other.TargetId)
               && JudgeId.Equals(other.JudgeId)
               && Reason == other.Reason;
    }

    public override bool Equals(object obj)
    {
        return obj is JudgePenaltyEvent other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine((int)PenaltyType, TargetId, JudgeId, Reason);
    }

    public static bool operator ==(JudgePenaltyEvent left, JudgePenaltyEvent right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(JudgePenaltyEvent left, JudgePenaltyEvent right)
    {
        return !left.Equals(right);
    }
}