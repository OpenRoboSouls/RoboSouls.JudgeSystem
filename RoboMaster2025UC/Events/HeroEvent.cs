using System;
using RoboSouls.JudgeSystem.Events;

namespace RoboSouls.JudgeSystem.RoboMaster2025UC.Events;

public readonly struct HeroEnterDeploymentModeEvent
    : IJudgeSystemEvent<HeroEnterDeploymentModeEvent>,
        IEquatable<HeroEnterDeploymentModeEvent>
{
    public readonly Identity HeroId;

    public HeroEnterDeploymentModeEvent(Identity heroId)
    {
        HeroId = heroId;
    }

    public bool Equals(HeroEnterDeploymentModeEvent other)
    {
        return HeroId.Equals(other.HeroId);
    }

    public override bool Equals(object obj)
    {
        return obj is HeroEnterDeploymentModeEvent other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HeroId.GetHashCode();
    }

    public static bool operator ==(
        HeroEnterDeploymentModeEvent left,
        HeroEnterDeploymentModeEvent right
    )
    {
        return left.Equals(right);
    }

    public static bool operator !=(
        HeroEnterDeploymentModeEvent left,
        HeroEnterDeploymentModeEvent right
    )
    {
        return !left.Equals(right);
    }
}

public readonly struct HeroExitDeploymentModeEvent
    : IJudgeSystemEvent<HeroExitDeploymentModeEvent>,
        IEquatable<HeroExitDeploymentModeEvent>
{
    public readonly Identity HeroId;

    public HeroExitDeploymentModeEvent(Identity heroId)
    {
        HeroId = heroId;
    }

    public bool Equals(HeroExitDeploymentModeEvent other)
    {
        return HeroId.Equals(other.HeroId);
    }

    public override bool Equals(object obj)
    {
        return obj is HeroExitDeploymentModeEvent other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HeroId.GetHashCode();
    }

    public static bool operator ==(
        HeroExitDeploymentModeEvent left,
        HeroExitDeploymentModeEvent right
    )
    {
        return left.Equals(right);
    }

    public static bool operator !=(
        HeroExitDeploymentModeEvent left,
        HeroExitDeploymentModeEvent right
    )
    {
        return !left.Equals(right);
    }
}