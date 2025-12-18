using System;
using RoboSouls.JudgeSystem.Events;
using RoboSouls.JudgeSystem.RoboMaster2025UC.Systems;

namespace RoboSouls.JudgeSystem.RoboMaster2025UC.Events;

/// <summary>
/// 能量机关开始
/// </summary>
public readonly struct PowerRuneStartEvent
    : IJudgeSystemEvent<PowerRuneStartEvent>,
        IEquatable<PowerRuneStartEvent>
{
    public readonly bool IsBigPowerRune;
    public readonly BigPowerRuneOptions Options;
    public readonly bool UseClockwise;

    public PowerRuneStartEvent(
        bool isBigPowerRune,
        BigPowerRuneOptions options,
        bool useClockwise
    )
    {
        IsBigPowerRune = isBigPowerRune;
        Options = options;
        UseClockwise = useClockwise;
    }

    public bool Equals(PowerRuneStartEvent other)
    {
        return IsBigPowerRune == other.IsBigPowerRune
               && Options.Equals(other.Options)
               && UseClockwise == other.UseClockwise;
    }

    public override bool Equals(object obj)
    {
        return obj is PowerRuneStartEvent other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(IsBigPowerRune, Options, UseClockwise);
    }

    public static bool operator ==(PowerRuneStartEvent left, PowerRuneStartEvent right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(PowerRuneStartEvent left, PowerRuneStartEvent right)
    {
        return !left.Equals(right);
    }
}

/// <summary>
/// 能量机关激活完成
/// </summary>
public readonly struct PowerRuneActivatedEvent
    : IJudgeSystemEvent<PowerRuneActivatedEvent>,
        IEquatable<PowerRuneActivatedEvent>
{
    public readonly bool IsBigPowerRune;
    public readonly PowerRuneActivateRecord Record;
    public readonly Camp Camp;

    public PowerRuneActivatedEvent(
        bool isBigPowerRune,
        PowerRuneActivateRecord record,
        Camp camp
    )
    {
        IsBigPowerRune = isBigPowerRune;
        Record = record;
        Camp = camp;
    }

    public bool Equals(PowerRuneActivatedEvent other)
    {
        return IsBigPowerRune == other.IsBigPowerRune
               && Record.Equals(other.Record)
               && Camp == other.Camp;
    }

    public override bool Equals(object obj)
    {
        return obj is PowerRuneActivatedEvent other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(IsBigPowerRune, Record, (int)Camp);
    }

    public static bool operator ==(PowerRuneActivatedEvent left, PowerRuneActivatedEvent right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(PowerRuneActivatedEvent left, PowerRuneActivatedEvent right)
    {
        return !left.Equals(right);
    }
}

/// <summary>
/// 能量机关被动停止
/// </summary>
public readonly struct PowerRuneStopEvent
    : IJudgeSystemEvent<PowerRuneStopEvent>,
        IEquatable<PowerRuneStopEvent>
{
    public readonly Camp Camp;

    public PowerRuneStopEvent(Camp camp)
    {
        Camp = camp;
    }

    public bool Equals(PowerRuneStopEvent other)
    {
        return Camp == other.Camp;
    }

    public override bool Equals(object obj)
    {
        return obj is PowerRuneStopEvent other && Equals(other);
    }

    public override int GetHashCode()
    {
        return (int)Camp;
    }

    public static bool operator ==(PowerRuneStopEvent left, PowerRuneStopEvent right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(PowerRuneStopEvent left, PowerRuneStopEvent right)
    {
        return !left.Equals(right);
    }
}