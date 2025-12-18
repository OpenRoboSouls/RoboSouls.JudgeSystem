using System;
using RoboSouls.JudgeSystem.Events;

namespace RoboSouls.JudgeSystem.RoboMaster2024UL.Events;

public struct CentralZoneOccupiedEvent
    : IJudgeSystemEvent<CentralZoneOccupiedEvent>,
        IEquatable<CentralZoneOccupiedEvent>
{
    public double Time;
    public Camp Camp;

    public bool Equals(CentralZoneOccupiedEvent other)
    {
        return Time.Equals(other.Time) && Camp == other.Camp;
    }

    public override bool Equals(object obj)
    {
        return obj is CentralZoneOccupiedEvent other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Time, (int)Camp);
    }
}