using System;
using RoboSouls.JudgeSystem.Events;

namespace RoboSouls.JudgeSystem.RoboMaster2025UC.Events;

public readonly struct DeployHitEvent
    : IJudgeSystemEvent<DeployHitEvent>,
        IEquatable<DeployHitEvent>
{
    public readonly Camp Camp;
    public readonly int HitCount;
    public readonly int HitCountAllowance;
    public readonly double Time;

    public DeployHitEvent(Camp camp, int hitCount, int hitCountAllowance, double time)
    {
        Camp = camp;
        HitCount = hitCount;
        HitCountAllowance = hitCountAllowance;
        Time = time;
    }

    public bool Equals(DeployHitEvent other)
    {
        return Camp == other.Camp
               && HitCount == other.HitCount
               && HitCountAllowance == other.HitCountAllowance
               && Time.Equals(other.Time);
    }

    public override bool Equals(object obj)
    {
        return obj is DeployHitEvent other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine((int)Camp, HitCount, HitCountAllowance, Time);
    }
}