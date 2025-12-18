using System;

namespace RoboSouls.JudgeSystem.Events;

/// <summary>
/// 升级
/// </summary>
public readonly struct LevelUpdateEvent(Identity @operator, int prevLevel, int newLevel)
    : IJudgeSystemEvent<LevelUpdateEvent>,
        IEquatable<LevelUpdateEvent>
{
    public readonly Identity Operator = @operator;
    public readonly int PrevLevel = prevLevel;
    public readonly int NewLevel = newLevel;

    public bool Equals(LevelUpdateEvent other)
    {
        return Operator.Equals(other.Operator)
               && PrevLevel == other.PrevLevel
               && NewLevel == other.NewLevel;
    }

    public override bool Equals(object? obj)
    {
        return obj is LevelUpdateEvent other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Operator, PrevLevel, NewLevel);
    }
}