using System;
using RoboSouls.JudgeSystem.Events;
using RoboSouls.JudgeSystem.RoboMaster2025UC.Systems;

namespace RoboSouls.JudgeSystem.RoboMaster2025UC.Events;

public readonly struct ExchangeOreSuccessEvent
    : IJudgeSystemEvent<ExchangeOreSuccessEvent>,
        IEquatable<ExchangeOreSuccessEvent>
{
    public readonly double StageTime;
    public readonly OreType OreType;
    public readonly int Level;
    public readonly double Duration;
    public readonly Identity EngineerId;
    public readonly int CoinGained;
    public readonly float Multiplier;

    public ExchangeOreSuccessEvent(
        double stageTime,
        OreType oreType,
        int level,
        double duration,
        Identity engineerId,
        int coinGained,
        float multiplier
    )
    {
        StageTime = stageTime;
        OreType = oreType;
        Level = level;
        Duration = duration;
        EngineerId = engineerId;
        CoinGained = coinGained;
        Multiplier = multiplier;
    }

    public bool Equals(ExchangeOreSuccessEvent other)
    {
        return StageTime.Equals(other.StageTime)
               && OreType == other.OreType
               && Level == other.Level
               && Duration.Equals(other.Duration)
               && EngineerId.Equals(other.EngineerId)
               && CoinGained == other.CoinGained
               && Multiplier.Equals(other.Multiplier);
    }

    public override bool Equals(object obj)
    {
        return obj is ExchangeOreSuccessEvent other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            StageTime,
            (int)OreType,
            Level,
            Duration,
            EngineerId,
            CoinGained,
            Multiplier
        );
    }
}

public readonly struct ExchangeStateChangeEvent
    : IJudgeSystemEvent<ExchangeStateChangeEvent>,
        IEquatable<ExchangeStateChangeEvent>
{
    public readonly ExchangerState OldState;
    public readonly ExchangerState NewState;
    public readonly Camp Camp;

    public ExchangeStateChangeEvent(ExchangerState oldState, ExchangerState newState, Camp camp)
    {
        OldState = oldState;
        NewState = newState;
        Camp = camp;
    }

    public bool Equals(ExchangeStateChangeEvent other)
    {
        return OldState == other.OldState && NewState == other.NewState && Camp == other.Camp;
    }

    public override bool Equals(object obj)
    {
        return obj is ExchangeStateChangeEvent other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine((int)OldState, (int)NewState, (int)Camp);
    }

    public static bool operator ==(
        ExchangeStateChangeEvent left,
        ExchangeStateChangeEvent right
    )
    {
        return left.Equals(right);
    }

    public static bool operator !=(
        ExchangeStateChangeEvent left,
        ExchangeStateChangeEvent right
    )
    {
        return !left.Equals(right);
    }
}