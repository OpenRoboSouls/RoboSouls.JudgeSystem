using System;
using RoboSouls.JudgeSystem.Events;

namespace RoboSouls.JudgeSystem.RoboMaster2025UC.Events;

/// <summary>
/// 占领敌方堡垒时间达到，基地护甲展开
/// </summary>
public readonly struct FortressOccupyBaseEvent
    : IJudgeSystemEvent<FortressOccupyBaseEvent>,
        IEquatable<FortressOccupyBaseEvent>
{
    public readonly Camp Camp;
    public readonly double Time;

    public FortressOccupyBaseEvent(Camp camp, double time)
    {
        Camp = camp;
        Time = time;
    }

    public bool Equals(FortressOccupyBaseEvent other)
    {
        return Camp == other.Camp && Time.Equals(other.Time);
    }

    public override bool Equals(object obj)
    {
        return obj is FortressOccupyBaseEvent other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine((int)Camp, Time);
    }
}