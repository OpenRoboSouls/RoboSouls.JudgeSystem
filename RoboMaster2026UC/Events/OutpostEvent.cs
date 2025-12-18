using System;
using RoboSouls.JudgeSystem.Events;

namespace RoboSouls.JudgeSystem.RoboMaster2026UC.Events;

public readonly struct OutpostRotateStartEvent
    : IJudgeSystemEvent<OutpostRotateStartEvent>,
        IEquatable<OutpostRotateStartEvent>
{
    /// <summary>
    /// 是否顺时针旋转
    /// </summary>
    public readonly bool Clockwise;

    /// <summary>
    /// 旋转速度 - rad/s
    /// </summary>
    public readonly float RotateSpeed;

    public readonly Camp Camp;

    public OutpostRotateStartEvent(Camp camp, bool clockwise, float rotateSpeed)
    {
        Clockwise = clockwise;
        RotateSpeed = rotateSpeed;
        Camp = camp;
    }

    public bool Equals(OutpostRotateStartEvent other)
    {
        return Clockwise == other.Clockwise
               && RotateSpeed.Equals(other.RotateSpeed)
               && Camp == other.Camp;
    }

    public override bool Equals(object obj)
    {
        return obj is OutpostRotateStartEvent other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Clockwise, RotateSpeed, (int)Camp);
    }

    public static bool operator ==(OutpostRotateStartEvent left, OutpostRotateStartEvent right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(OutpostRotateStartEvent left, OutpostRotateStartEvent right)
    {
        return !left.Equals(right);
    }
}

public readonly struct OutpostRotateStopEvent
    : IJudgeSystemEvent<OutpostRotateStopEvent>,
        IEquatable<OutpostRotateStopEvent>
{
    public readonly Camp Camp;

    public OutpostRotateStopEvent(Camp camp)
    {
        Camp = camp;
    }

    public bool Equals(OutpostRotateStopEvent other)
    {
        return Camp == other.Camp;
    }

    public override bool Equals(object obj)
    {
        return obj is OutpostRotateStopEvent other && Equals(other);
    }

    public override int GetHashCode()
    {
        return (int)Camp;
    }

    public static bool operator ==(OutpostRotateStopEvent left, OutpostRotateStopEvent right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(OutpostRotateStopEvent left, OutpostRotateStopEvent right)
    {
        return !left.Equals(right);
    }
}
    
public readonly struct OutpostRebuiltEvent
    : IJudgeSystemEvent<OutpostRebuiltEvent>,
        IEquatable<OutpostRebuiltEvent>
{
    public readonly Camp Camp;

    public OutpostRebuiltEvent(Camp camp)
    {
        Camp = camp;
    }

    public bool Equals(OutpostRebuiltEvent other)
    {
        return Camp == other.Camp;
    }

    public override bool Equals(object obj)
    {
        return obj is OutpostRebuiltEvent other && Equals(other);
    }

    public override int GetHashCode()
    {
        return (int)Camp;
    }

    public static bool operator ==(OutpostRebuiltEvent left, OutpostRebuiltEvent right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(OutpostRebuiltEvent left, OutpostRebuiltEvent right)
    {
        return !left.Equals(right);
    }
}