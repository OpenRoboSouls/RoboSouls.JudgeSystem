namespace RoboSouls.JudgeSystem.Entities;

public abstract class RobotBase(Identity id) : IRobot
{
    public Identity Id { get; } = id;
}