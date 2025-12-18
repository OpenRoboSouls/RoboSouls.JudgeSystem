namespace RoboSouls.JudgeSystem.Systems;

public interface IScoreSystem : ISystem
{
    public int GetScore(in Identity id);
}