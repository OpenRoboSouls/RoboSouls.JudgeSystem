using System;
using RoboSouls.JudgeSystem.Events;
using RoboSouls.JudgeSystem.RoboMaster2026UC.Systems;

namespace RoboSouls.JudgeSystem.RoboMaster2026UC.Events;

/// <summary>
/// 飞镖发射站开启事件
/// </summary>
public readonly record struct DartStationOpenEvent(Camp Camp, DartTarget Target)
    : IJudgeSystemEvent<DartStationOpenEvent>;

/// <summary>
/// 飞镖发射站关闭事件
/// </summary>
public readonly record struct DartStationCloseEvent(Camp Camp)
    : IJudgeSystemEvent<DartStationCloseEvent>;

/// <summary>
/// 飞镖命中
/// </summary>
public readonly record struct DartHitEvent(Camp Camp, DartTarget Target)
    : IJudgeSystemEvent<DartHitEvent>;

/// <summary>
/// 飞镖发射事件
/// </summary>
public readonly record struct DartLaunchEvent(DartTarget Target, Identity DartId, double Time)
    : IJudgeSystemEvent<DartLaunchEvent>;