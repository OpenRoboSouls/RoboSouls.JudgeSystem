using System;
using RoboSouls.JudgeSystem.Events;

namespace RoboSouls.JudgeSystem.RoboMaster2025UC.Events;

/// <summary>
///     空中支援开始
/// </summary>
public readonly struct AirstrikeStartEvent
    : IJudgeSystemEvent<AirstrikeStartEvent>,
        IEquatable<AirstrikeStartEvent>
{
    public readonly Identity AerialId;
    public readonly double Time;

    public AirstrikeStartEvent(Identity aerialId, double time)
    {
        AerialId = aerialId;
        Time = time;
    }

    public bool Equals(AirstrikeStartEvent other)
    {
        return AerialId.Equals(other.AerialId) && Time.Equals(other.Time);
    }

    public override bool Equals(object obj)
    {
        return obj is AirstrikeStartEvent other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(AerialId, Time);
    }

    public static bool operator ==(AirstrikeStartEvent left, AirstrikeStartEvent right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(AirstrikeStartEvent left, AirstrikeStartEvent right)
    {
        return !left.Equals(right);
    }
}

/// <summary>
///     空中支援结束
/// </summary>
public readonly struct AirstrikeStopEvent
    : IJudgeSystemEvent<AirstrikeStopEvent>,
        IEquatable<AirstrikeStopEvent>
{
    public readonly Identity AerialId;
    public readonly double Time;

    public AirstrikeStopEvent(Identity aerialId, double time)
    {
        AerialId = aerialId;
        Time = time;
    }

    public bool Equals(AirstrikeStopEvent other)
    {
        return AerialId.Equals(other.AerialId) && Time.Equals(other.Time);
    }

    public override bool Equals(object obj)
    {
        return obj is AirstrikeStopEvent other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(AerialId, Time);
    }

    public static bool operator ==(AirstrikeStopEvent left, AirstrikeStopEvent right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(AirstrikeStopEvent left, AirstrikeStopEvent right)
    {
        return !left.Equals(right);
    }
}