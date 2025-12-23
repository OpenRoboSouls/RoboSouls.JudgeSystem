using System;
using RoboSouls.JudgeSystem.Events;

namespace RoboSouls.JudgeSystem.RoboMaster2026UC.Events;

public readonly record struct AssemblySuccessEvent(Camp Camp, int Level, bool IsFirst) : IJudgeSystemEvent<AssemblySuccessEvent>;