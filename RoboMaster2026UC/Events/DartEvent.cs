using System;
using RoboSouls.JudgeSystem.Events;
using RoboSouls.JudgeSystem.RoboMaster2026UC.Systems;

namespace RoboSouls.JudgeSystem.RoboMaster2026UC.Events;

/// <summary>
/// 飞镖发射站开启事件
/// </summary>
public readonly struct DartStationOpenEvent
    : IJudgeSystemEvent<DartStationOpenEvent>,
        IEquatable<DartStationOpenEvent>
{
    public readonly Camp Camp;
    public readonly DartTarget Target;

    public DartStationOpenEvent(Camp camp, DartTarget target)
    {
        Camp = camp;
        Target = target;
    }

    public bool Equals(DartStationOpenEvent other)
    {
        return Camp == other.Camp && Target == other.Target;
    }

    public override bool Equals(object obj)
    {
        return obj is DartStationOpenEvent other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine((int)Camp, (int)Target);
    }

    public static bool operator ==(DartStationOpenEvent left, DartStationOpenEvent right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(DartStationOpenEvent left, DartStationOpenEvent right)
    {
        return !left.Equals(right);
    }
}

/// <summary>
/// 飞镖发射站关闭事件
/// </summary>
public readonly struct DartStationCloseEvent
    : IJudgeSystemEvent<DartStationCloseEvent>,
        IEquatable<DartStationCloseEvent>
{
    public readonly Camp Camp;

    public DartStationCloseEvent(Camp camp)
    {
        Camp = camp;
    }

    public bool Equals(DartStationCloseEvent other)
    {
        return Camp == other.Camp;
    }

    public override bool Equals(object obj)
    {
        return obj is DartStationCloseEvent other && Equals(other);
    }

    public override int GetHashCode()
    {
        return (int)Camp;
    }

    public static bool operator ==(DartStationCloseEvent left, DartStationCloseEvent right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(DartStationCloseEvent left, DartStationCloseEvent right)
    {
        return !left.Equals(right);
    }
}

/// <summary>
/// 飞镖命中
/// </summary>
public readonly struct DartHitEvent : IJudgeSystemEvent<DartHitEvent>, IEquatable<DartHitEvent>
{
    public readonly Camp Camp;
    public readonly DartTarget Target;

    public DartHitEvent(Camp camp, DartTarget target)
    {
        Camp = camp;
        Target = target;
    }

    public bool Equals(DartHitEvent other)
    {
        return Camp == other.Camp && Target == other.Target;
    }

    public override bool Equals(object obj)
    {
        return obj is DartHitEvent other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine((int)Camp, (int)Target);
    }

    public static bool operator ==(DartHitEvent left, DartHitEvent right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(DartHitEvent left, DartHitEvent right)
    {
        return !left.Equals(right);
    }
}

/// <summary>
/// 飞镖发射事件
/// </summary>
public readonly struct DartLaunchEvent
    : IJudgeSystemEvent<DartLaunchEvent>,
        IEquatable<DartLaunchEvent>
{
    public readonly DartTarget Target;
    public readonly Identity DartId;
    public readonly double Time;

    public DartLaunchEvent(DartTarget target, Identity dartId, double time)
    {
        Target = target;
        DartId = dartId;
        Time = time;
    }

    public bool Equals(DartLaunchEvent other)
    {
        return Target == other.Target && DartId.Equals(other.DartId) && Time.Equals(other.Time);
    }

    public override bool Equals(object obj)
    {
        return obj is DartLaunchEvent other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine((int)Target, DartId, Time);
    }
}