using RoboSouls.JudgeSystem.Events;

namespace RoboSouls.JudgeSystem.RoboMaster2026UC.Events;

public readonly record struct BaseArmorOpenEvent(Identity BaseId) : IJudgeSystemEvent<BaseArmorOpenEvent>;