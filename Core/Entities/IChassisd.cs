namespace RoboSouls.JudgeSystem.Entities;

public interface IChassisd : IEntity
{
    public static readonly int ChassisTypeCacheKey = "ChassisType".Sum();
    public byte ChassisType { get; }
}