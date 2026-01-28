using RoboSouls.JudgeSystem.Events;

namespace RoboSouls.JudgeSystem.RoboMaster2026UC.Events;

public readonly record struct OutpostRotateStartEvent(Camp Camp, bool Clockwise, float RotateSpeed)
    : IJudgeSystemEvent<OutpostRotateStartEvent>;

public readonly record struct OutpostRotateStopEvent(Camp Camp)
    : IJudgeSystemEvent<OutpostRotateStopEvent>;

public readonly record struct OutpostRebuiltEvent(Camp Camp)
    : IJudgeSystemEvent<OutpostRebuiltEvent>;