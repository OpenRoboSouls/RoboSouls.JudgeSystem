using RoboSouls.JudgeSystem.Entities;
using VContainer;

namespace RoboSouls.JudgeSystem.RoboMaster2025UC.Entities;

public class Engineer(Identity id) : RobotBase(id), IHealthed
{
    [Inject]
    internal ICacheReader<uint> UintCacheBox { get; set; }
    public uint Health => UintCacheBox.WithReaderNamespace(Id).Load(IHealthed.HealthCacheKey);
}