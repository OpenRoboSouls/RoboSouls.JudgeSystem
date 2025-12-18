namespace RoboSouls.JudgeSystem.RoboMaster2025UC;

public interface IMatchConfigurationRM2025uc : IMatchConfiguration
{
    public float GetDartHitRate(Camp camp);
    public int GetInitialCoin(Camp camp);
}