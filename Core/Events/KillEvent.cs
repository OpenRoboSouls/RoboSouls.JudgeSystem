using System;

namespace RoboSouls.JudgeSystem.Events;

/// <summary>
/// 击杀
/// </summary>
public readonly struct KillEvent(double time, Identity killer, Identity victim)
    : IJudgeSystemEvent<KillEvent>, IEquatable<KillEvent>
{
    public readonly double Time = time;
    public readonly Identity Killer = killer;
    public readonly Identity Victim = victim;

    public bool Equals(KillEvent other)
    {
        return Time.Equals(other.Time) && Killer.Equals(other.Killer) && Victim.Equals(other.Victim);
    }

    public override bool Equals(object? obj)
    {
        return obj is KillEvent other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Time, Killer, Victim);
    }
}

public struct ReviveEvent : IJudgeSystemEvent<ReviveEvent>, IEquatable<ReviveEvent>
{
    public double Time;
    public Identity Reviver;

    public bool Equals(ReviveEvent other)
    {
        return Time.Equals(other.Time) && Reviver.Equals(other.Reviver);
    }

    public override bool Equals(object? obj)
    {
        return obj is ReviveEvent other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Time, Reviver);
    }
}