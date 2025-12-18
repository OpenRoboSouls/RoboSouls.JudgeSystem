using RoboSouls.JudgeSystem.Entities;
using VContainer;

namespace RoboSouls.JudgeSystem.RoboMaster2026UC.Entities;

public class Base : IBuilding, IHealthed
{
    public static readonly int ArmorOpenCacheKey = "shield_open".Sum();

    public Base(Identity id)
    {
        Id = id;
    }

    [Inject]
    internal ICacheReader<uint> UintCacheBox { get; set; }

    [Inject]
    internal ICacheReader<bool> BoolCacheBox { get; set; }
    public bool IsArmorOpen => BoolCacheBox.WithReaderNamespace(Id).Load(ArmorOpenCacheKey);

    public Identity Id { get; }

    public uint Health => UintCacheBox.WithReaderNamespace(Id).Load(IHealthed.HealthCacheKey);
}