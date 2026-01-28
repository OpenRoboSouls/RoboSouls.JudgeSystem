using RoboSouls.JudgeSystem.Entities;
using VitalRouter;

namespace RoboSouls.JudgeSystem.Events;

public readonly record struct DamageCommand(
    IShooter Shooter,
    IHealthed Victim,
    uint Damage,
    byte AmmoType,
    byte ArmorType,
    byte ArmorId)
    : ICommand;