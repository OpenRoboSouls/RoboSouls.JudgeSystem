using System;
using RoboSouls.JudgeSystem.Events;

namespace RoboSouls.JudgeSystem.RoboMaster2025UC.Events;

/// <summary>
///     堡垒增益点生效
/// </summary>
public readonly struct FortressActivateEvent
    : IJudgeSystemEvent<FortressActivateEvent>,
        IEquatable<FortressActivateEvent>
{
    public readonly Camp Camp;

    public FortressActivateEvent(Camp camp)
    {
        Camp = camp;
    }

    public bool Equals(FortressActivateEvent other)
    {
        return Camp == other.Camp;
    }

    public override bool Equals(object obj)
    {
        return obj is FortressActivateEvent other && Equals(other);
    }

    public override int GetHashCode()
    {
        return (int)Camp;
    }

    public static bool operator ==(FortressActivateEvent left, FortressActivateEvent right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(FortressActivateEvent left, FortressActivateEvent right)
    {
        return !left.Equals(right);
    }
}

/// <summary>
///     使用堡垒增益点
/// </summary>
public readonly struct FortressEnterEvent
    : IJudgeSystemEvent<FortressEnterEvent>,
        IEquatable<FortressEnterEvent>
{
    public readonly Identity Operator;

    public FortressEnterEvent(Identity @operator)
    {
        Operator = @operator;
    }

    public bool Equals(FortressEnterEvent other)
    {
        return Operator.Equals(other.Operator);
    }

    public override bool Equals(object obj)
    {
        return obj is FortressEnterEvent other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Operator.GetHashCode();
    }

    public static bool operator ==(FortressEnterEvent left, FortressEnterEvent right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(FortressEnterEvent left, FortressEnterEvent right)
    {
        return !left.Equals(right);
    }
}

/// <summary>
///     离开堡垒增益点
/// </summary>
public readonly struct FortressExitEvent
    : IJudgeSystemEvent<FortressExitEvent>,
        IEquatable<FortressExitEvent>
{
    public readonly Identity Operator;

    public FortressExitEvent(Identity @operator)
    {
        Operator = @operator;
    }

    public bool Equals(FortressExitEvent other)
    {
        return Operator.Equals(other.Operator);
    }

    public override bool Equals(object obj)
    {
        return obj is FortressExitEvent other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Operator.GetHashCode();
    }

    public static bool operator ==(FortressExitEvent left, FortressExitEvent right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(FortressExitEvent left, FortressExitEvent right)
    {
        return !left.Equals(right);
    }
}