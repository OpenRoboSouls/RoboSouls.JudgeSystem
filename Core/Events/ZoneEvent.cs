using System;

namespace RoboSouls.JudgeSystem.Events;

public interface IZoneEvent
{
    Identity ZoneIdentity { get; }
}

/// <summary>
/// 进入区域
/// </summary>
public readonly struct EnterZoneEvent(Identity zoneId, Identity operatorId) : IZoneEvent,
    IJudgeSystemEvent<EnterZoneEvent>,
    IEquatable<EnterZoneEvent>
{
    public readonly Identity ZoneId = zoneId;
    public readonly Identity OperatorId = operatorId;
    public Identity ZoneIdentity => ZoneId;

    public bool Equals(EnterZoneEvent other)
    {
        return ZoneId.Equals(other.ZoneId) && OperatorId.Equals(other.OperatorId);
    }

    public override bool Equals(object obj)
    {
        return obj is EnterZoneEvent other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ZoneId, OperatorId);
    }

    public static bool operator ==(EnterZoneEvent left, EnterZoneEvent right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(EnterZoneEvent left, EnterZoneEvent right)
    {
        return !left.Equals(right);
    }
}

/// <summary>
/// 离开区域
/// </summary>
public struct ExitZoneEvent
    : IZoneEvent,
        IJudgeSystemEvent<ExitZoneEvent>,
        IEquatable<ExitZoneEvent>
{
    public Identity ZoneId;
    public Identity OperatorId;

    public Identity ZoneIdentity => ZoneId;

    public bool Equals(ExitZoneEvent other)
    {
        return ZoneId.Equals(other.ZoneId) && OperatorId.Equals(other.OperatorId);
    }

    public override bool Equals(object obj)
    {
        return obj is ExitZoneEvent other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ZoneId, OperatorId);
    }

    public static bool operator ==(ExitZoneEvent left, ExitZoneEvent right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ExitZoneEvent left, ExitZoneEvent right)
    {
        return !left.Equals(right);
    }
}