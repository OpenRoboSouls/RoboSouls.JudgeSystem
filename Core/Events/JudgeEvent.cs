using RoboSouls.JudgeSystem.Systems;

namespace RoboSouls.JudgeSystem.Events;

public readonly record struct JudgePenaltyEvent(
    PenaltyType PenaltyType,
    Identity TargetId,
    Identity JudgeId,
    byte Reason)
    : IJudgeSystemEvent<JudgePenaltyEvent>;