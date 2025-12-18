using RoboSouls.JudgeSystem.Entities;
using VContainer;

namespace RoboSouls.JudgeSystem.RoboMaster2026UC.Entities;

public class Engineer : RobotBase, IHealthed
{
    public Engineer(Identity id)
        : base(id) { }

    [Inject]
    internal ICacheReader<uint> UintCacheBox { get; set; }
    public uint Health => UintCacheBox.WithReaderNamespace(Id).Load(IHealthed.HealthCacheKey);
}