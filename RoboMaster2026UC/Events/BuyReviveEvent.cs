using System;
using RoboSouls.JudgeSystem.Events;

namespace RoboSouls.JudgeSystem.RoboMaster2026UC.Events;

public readonly struct BuyReviveEvent
    : IJudgeSystemEvent<BuyReviveEvent>,
        IEquatable<BuyReviveEvent>
{
    public readonly Identity Id;
    public readonly int Cost;
    public readonly double Time;

    public BuyReviveEvent(Identity id, int cost, double time)
    {
        Id = id;
        Cost = cost;
        Time = time;
    }

    public bool Equals(BuyReviveEvent other)
    {
        return Id.Equals(other.Id) && Cost == other.Cost && Time.Equals(other.Time);
    }

    public override bool Equals(object obj)
    {
        return obj is BuyReviveEvent other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Id, Cost, Time);
    }

    public static bool operator ==(BuyReviveEvent left, BuyReviveEvent right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(BuyReviveEvent left, BuyReviveEvent right)
    {
        return !left.Equals(right);
    }
}