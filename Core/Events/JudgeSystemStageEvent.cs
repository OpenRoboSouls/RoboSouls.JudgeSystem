using System;

namespace RoboSouls.JudgeSystem.Events;

public readonly struct JudgeSystemStageChangedEvent(JudgeSystemStage prev, JudgeSystemStage next)
    : IJudgeSystemEvent<JudgeSystemStageChangedEvent>,
        IEquatable<JudgeSystemStageChangedEvent>
{
    public readonly JudgeSystemStage Prev = prev;
    public readonly JudgeSystemStage Next = next;

    public bool Equals(JudgeSystemStageChangedEvent other)
    {
        return Prev == other.Prev && Next == other.Next;
    }

    public override bool Equals(object obj)
    {
        return obj is JudgeSystemStageChangedEvent other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine((int)Prev, (int)Next);
    }

    public static bool operator ==(
        JudgeSystemStageChangedEvent left,
        JudgeSystemStageChangedEvent right
    )
    {
        return left.Equals(right);
    }

    public static bool operator !=(
        JudgeSystemStageChangedEvent left,
        JudgeSystemStageChangedEvent right
    )
    {
        return !left.Equals(right);
    }
}