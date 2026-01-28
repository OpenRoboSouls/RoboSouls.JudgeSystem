using RoboSouls.JudgeSystem.Entities;
using VContainer;

namespace RoboSouls.JudgeSystem.RoboMaster2024UL.Entities;

public class Base : IBuilding, IHealthed
{
    public static readonly int ShieldCacheKey = "shield".Sum();

    public Base(Identity id)
    {
        Id = id;
    }

    [Inject] internal ICacheReader<uint> UintCacheBox { get; set; }

    public uint Shield => UintCacheBox.WithReaderNamespace(Id).Load(ShieldCacheKey);

    public Identity Id { get; }

    public uint Health => UintCacheBox.WithReaderNamespace(Id).Load(IHealthed.HealthCacheKey);
}