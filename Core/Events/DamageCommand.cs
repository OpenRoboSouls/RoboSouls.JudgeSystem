using RoboSouls.JudgeSystem.Entities;
using VitalRouter;

namespace RoboSouls.JudgeSystem.Events;

public readonly struct DamageCommand(
    IShooter shooter,
    IHealthed victim,
    uint damage,
    byte ammoType,
    byte armorType,
    byte armorId)
    : ICommand
{
    public readonly IShooter Shooter = shooter;
    public readonly IHealthed Victim = victim;
    public readonly uint Damage = damage;
    public readonly byte AmmoType = ammoType;
    public readonly byte ArmorType = armorType;
    public readonly byte ArmorId = armorId;
}

