using System;
using RoboSouls.JudgeSystem.Events;

namespace RoboSouls.JudgeSystem.RoboMaster2026UC.Events;

public readonly record struct HeroEnterDeploymentModeEvent(Identity HeroId)
    : IJudgeSystemEvent<HeroEnterDeploymentModeEvent>;


public readonly record struct HeroExitDeploymentModeEvent(Identity HeroId)
    : IJudgeSystemEvent<HeroExitDeploymentModeEvent>;