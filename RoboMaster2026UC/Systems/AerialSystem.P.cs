namespace RoboSouls.JudgeSystem.RoboMaster2026UC.Systems;

partial class AerialSystem
{
    public partial int RadarCounterCount
    {
        get => intCache.Load(1);
        private set => intCache.Save(1, value);
    }

    public int GetRadarCounterCount(Identity id) => intCache.WithReaderNamespace(id).Load(1);
    private void SetRadarCounterCount(Identity id, int value) => intCache.WithWriterNamespace(id).Save(1, value);

    public int GetRadarCounterCount(Camp camp) => intCache.WithReaderNamespace(new Identity(camp, 0)).Load(1);
    private void SetRadarCounterCount(Camp camp, int value) => intCache.WithWriterNamespace(new Identity(camp, 0)).Save(1, value);
}