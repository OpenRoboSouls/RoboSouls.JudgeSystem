using RoboSouls.JudgeSystem.Entities;
using VitalRouter;

namespace RoboSouls.JudgeSystem.Events;

public readonly struct ShootCommand(IShooter shooter, int amount) : ICommand
{
    public readonly IShooter Shooter = shooter;
    public readonly int Amount = amount;
}