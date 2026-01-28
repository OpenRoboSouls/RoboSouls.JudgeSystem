using RoboSouls.JudgeSystem.Events;
using RoboSouls.JudgeSystem.RoboMaster2026UC.Systems;

namespace RoboSouls.JudgeSystem.RoboMaster2026UC.Events;

/// <summary>
///     能量机关开始
/// </summary>
public readonly record struct PowerRuneStartEvent(bool IsBigPowerRune, BigPowerRuneOptions Options, bool UseClockwise)
    : IJudgeSystemEvent<PowerRuneStartEvent>;

/// <summary>
///     能量机关激活完成
/// </summary>
public readonly record struct PowerRuneActivatedEvent(bool IsBigPowerRune, PowerRuneActivateRecord Record, Camp Camp)
    : IJudgeSystemEvent<PowerRuneActivatedEvent>;

/// <summary>
///     能量机关被动停止
/// </summary>
public readonly record struct PowerRuneStopEvent(Camp Camp)
    : IJudgeSystemEvent<PowerRuneStopEvent>;