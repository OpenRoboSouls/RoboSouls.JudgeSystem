using RoboSouls.JudgeSystem.Events;

namespace RoboSouls.JudgeSystem.RoboMaster2026UC.Events;

public readonly record struct DeployHitEvent(Camp Camp, int HitCount, int HitCountAllowance, double Time)
    : IJudgeSystemEvent<DeployHitEvent>;