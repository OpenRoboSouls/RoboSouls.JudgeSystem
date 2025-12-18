namespace RoboSouls.JudgeSystem.Entities;

public interface IHealthed : IEntity
{
    public static readonly int HealthCacheKey = "health".Sum();
    public uint Health { get; }
}

public static class HealthedExtensions
{
    public static bool IsDead(this IHealthed healthed)
    {
        return healthed.Health == 0;
    }
}