using RoboSouls.JudgeSystem.Events;

namespace RoboSouls.JudgeSystem.RoboMaster2026UC.Events;

/// <summary>
///     堡垒增益点生效
/// </summary>
public readonly record struct FortressActivateEvent(Camp Camp) : IJudgeSystemEvent<FortressActivateEvent>;

/// <summary>
///     使用堡垒增益点
/// </summary>
public readonly record struct FortressEnterEvent(Identity Operator) : IJudgeSystemEvent<FortressEnterEvent>;

/// <summary>
///     离开堡垒增益点
/// </summary>
public readonly record struct FortressExitEvent(Identity Operator) : IJudgeSystemEvent<FortressExitEvent>;

/// <summary>
///     占领敌方堡垒时间达到，基地护甲展开
/// </summary>
public readonly record struct FortressOccupyBaseEvent(Camp Camp, double Time)
    : IJudgeSystemEvent<FortressOccupyBaseEvent>;