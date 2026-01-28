namespace RoboSouls.JudgeSystem.Events;

/// <summary>
///     升级
/// </summary>
public readonly record struct LevelUpdateEvent(Identity Operator, int PrevLevel, int NewLevel)
    : IJudgeSystemEvent<LevelUpdateEvent>;