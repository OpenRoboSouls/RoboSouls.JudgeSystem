using System;
using RoboSouls.JudgeSystem.Events;

namespace RoboSouls.JudgeSystem.RoboMaster2026UC.Events;

public readonly record struct BuyReviveEvent(Identity Id, int Cost, double Time) : IJudgeSystemEvent<BuyReviveEvent>;