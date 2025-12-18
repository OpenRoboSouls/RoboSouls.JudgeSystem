namespace RoboSouls.JudgeSystem.Entities;

public interface IExperienced : IEntity
{
    public static readonly int ExpCacheKey = "Exp".Sum();
    public int Experience { get; }
}