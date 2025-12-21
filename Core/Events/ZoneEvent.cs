namespace RoboSouls.JudgeSystem.Events;

public interface IZoneEvent
{
    Identity ZoneIdentity { get; }
}

/// <summary>
/// 进入区域
/// </summary>
public readonly record struct EnterZoneEvent(Identity ZoneId, Identity OperatorId) : IZoneEvent,
    IJudgeSystemEvent<EnterZoneEvent>
{
    public Identity ZoneIdentity => ZoneId;
}

/// <summary>
/// 离开区域
/// </summary>
public readonly record struct ExitZoneEvent(Identity ZoneId, Identity OperatorId) : IZoneEvent,
    IJudgeSystemEvent<ExitZoneEvent>
{
    public Identity ZoneIdentity => ZoneId;
}