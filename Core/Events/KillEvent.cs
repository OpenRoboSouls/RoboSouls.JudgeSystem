namespace RoboSouls.JudgeSystem.Events;

/// <summary>
///     击杀
/// </summary>
public readonly record struct KillEvent(double Time, Identity Killer, Identity Victim)
    : IJudgeSystemEvent<KillEvent>;

public readonly record struct ReviveEvent(double Time, Identity Reviver)
    : IJudgeSystemEvent<ReviveEvent>;