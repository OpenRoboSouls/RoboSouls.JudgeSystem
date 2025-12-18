namespace RoboSouls.JudgeSystem.RoboMaster2026UC;

public interface IMatchConfigurationRM2026uc : IMatchConfiguration
{
    public float GetDartHitRate(Camp camp);
    public int GetInitialCoin(Camp camp);
}