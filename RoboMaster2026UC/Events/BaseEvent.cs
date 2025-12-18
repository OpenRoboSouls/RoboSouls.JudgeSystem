using System;
using RoboSouls.JudgeSystem.Events;

namespace RoboSouls.JudgeSystem.RoboMaster2026UC.Events;

public readonly struct BaseArmorOpenEvent
    : IJudgeSystemEvent<BaseArmorOpenEvent>,
        IEquatable<BaseArmorOpenEvent>
{
    public readonly Identity BaseId;

    public BaseArmorOpenEvent(Identity baseId)
    {
        BaseId = baseId;
    }

    public bool Equals(BaseArmorOpenEvent other)
    {
        return BaseId.Equals(other.BaseId);
    }

    public override bool Equals(object obj)
    {
        return obj is BaseArmorOpenEvent other && Equals(other);
    }

    public override int GetHashCode()
    {
        return BaseId.GetHashCode();
    }

    public static bool operator ==(BaseArmorOpenEvent left, BaseArmorOpenEvent right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(BaseArmorOpenEvent left, BaseArmorOpenEvent right)
    {
        return !left.Equals(right);
    }
}