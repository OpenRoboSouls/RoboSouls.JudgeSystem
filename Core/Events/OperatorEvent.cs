using System;

namespace RoboSouls.JudgeSystem.Events;

/// <summary>
/// 机器人登录
/// </summary>
public readonly struct OperatorLoginEvent(Identity id) : IJudgeSystemEvent<OperatorLoginEvent>,
    IEquatable<OperatorLoginEvent>
{
    public readonly Identity Id = id;

    public bool Equals(OperatorLoginEvent other)
    {
        return Id.Equals(other.Id);
    }

    public override bool Equals(object obj)
    {
        return obj is OperatorLoginEvent other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
}

/// <summary>
/// 机器人登出
/// </summary>
public readonly struct OperatorLogoutEvent(Identity id) : IJudgeSystemEvent<OperatorLogoutEvent>,
    IEquatable<OperatorLogoutEvent>
{
    public readonly Identity Id = id;

    public bool Equals(OperatorLogoutEvent other)
    {
        return Id.Equals(other.Id);
    }

    public override bool Equals(object obj)
    {
        return obj is OperatorLogoutEvent other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
}