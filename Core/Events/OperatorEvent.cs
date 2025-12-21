namespace RoboSouls.JudgeSystem.Events;

/// <summary>
/// 机器人登录
/// </summary>
public readonly record struct OperatorLoginEvent(Identity Id) : IJudgeSystemEvent<OperatorLoginEvent>;

/// <summary>
/// 机器人登出
/// </summary>
public readonly record struct OperatorLogoutEvent(Identity Id) : IJudgeSystemEvent<OperatorLogoutEvent>;