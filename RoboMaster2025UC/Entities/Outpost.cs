using RoboSouls.JudgeSystem.Entities;
using VContainer;

namespace RoboSouls.JudgeSystem.RoboMaster2025UC.Entities;

public class Outpost : IBuilding, IHealthed
{
    public static readonly int RotateClockwiseCacheKey = "RotateClockwise".GetHashCode();
    public static readonly int RotateSpeedCacheKey = "RotateSpeed".GetHashCode();

    public Outpost(Identity id)
    {
        Id = id;
    }

    [Inject] internal ICacheReader<uint> UintCacheBox { get; set; }

    [Inject] internal ICacheReader<bool> BoolCacheBox { get; set; }

    [Inject] internal ICacheProvider<float> FloatCacheBox { get; set; }

    public bool IsRotateClockwise =>
        BoolCacheBox.WithReaderNamespace(Id).Load(RotateClockwiseCacheKey);

    public float RotateSpeed => FloatCacheBox.WithReaderNamespace(Id).Load(RotateSpeedCacheKey);

    public Identity Id { get; }
    public uint Health => UintCacheBox.WithReaderNamespace(Id).Load(IHealthed.HealthCacheKey);
}