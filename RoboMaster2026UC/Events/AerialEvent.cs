using System;
using RoboSouls.JudgeSystem.Events;

namespace RoboSouls.JudgeSystem.RoboMaster2026UC.Events;

/// <summary>
/// 空中支援开始
/// </summary>
public readonly struct AirstrikeStartEvent(Identity aerialId, double time) : IJudgeSystemEvent<AirstrikeStartEvent>,
    IEquatable<AirstrikeStartEvent>
{
    public Identity AerialId => aerialId;
    public double Time => time;

    public bool Equals(AirstrikeStartEvent other)
    {
        return AerialId.Equals(other.AerialId) && Time.Equals(other.Time);
    }

    public override bool Equals(object? obj)
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
/// 空中支援结束
/// </summary>
public readonly struct AirstrikeStopEvent(Identity aerialId, double time) : IJudgeSystemEvent<AirstrikeStopEvent>,
    IEquatable<AirstrikeStopEvent>
{
    public Identity AerialId => aerialId;
    public double Time => time;

    public bool Equals(AirstrikeStopEvent other)
    {
        return AerialId.Equals(other.AerialId) && Time.Equals(other.Time);
    }

    public override bool Equals(object? obj)
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