using RoboSouls.JudgeSystem.Entities;
using VitalRouter;

namespace RoboSouls.JudgeSystem.Events;

public readonly record struct ShootCommand(IShooter Shooter, int Amount) : ICommand;