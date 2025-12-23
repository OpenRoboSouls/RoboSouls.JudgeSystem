using System;
using RoboSouls.JudgeSystem.Events;

namespace RoboSouls.JudgeSystem.RoboMaster2026UC.Events;

public readonly record struct AirstrikeStartEvent(Identity AerialId, double Time) : IJudgeSystemEvent<AirstrikeStartEvent>;

public readonly record struct AirstrikeStopEvent(Identity AerialId, double Time) : IJudgeSystemEvent<AirstrikeStopEvent>;