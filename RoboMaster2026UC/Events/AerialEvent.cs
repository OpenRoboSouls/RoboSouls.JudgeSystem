using RoboSouls.JudgeSystem.Events;

namespace RoboSouls.JudgeSystem.RoboMaster2026UC.Events;

public readonly record struct AirstrikeStartEvent(Identity AerialId, double Time)
    : IJudgeSystemEvent<AirstrikeStartEvent>;

public readonly record struct AirstrikeStopEvent(Identity AerialId, double Time)
    : IJudgeSystemEvent<AirstrikeStopEvent>;

/// <summary>
///     无人机被雷达锁定
/// </summary>
/// <param name="AerialId"></param>
/// <param name="Time"></param>
public readonly record struct AerialLockedEvent(Identity AerialId, double Time) : IJudgeSystemEvent<AerialLockedEvent>;

/// <summary>
///     雷达丢失无人机锁定
/// </summary>
/// <param name="AerialId"></param>
/// <param name="Time"></param>
public readonly record struct AerialLockStoppedEvent(Identity AerialId, double Time)
    : IJudgeSystemEvent<AerialLockStoppedEvent>;

/// <summary>
///     无人机被压制
/// </summary>
/// <param name="AerialId"></param>
/// <param name="Time"></param>
public readonly record struct AerialCounteredEvent(Identity AerialId, double Time)
    : IJudgeSystemEvent<AerialCounteredEvent>;