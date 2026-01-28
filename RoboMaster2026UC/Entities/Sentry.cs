using RoboSouls.JudgeSystem.Entities;
using RoboSouls.JudgeSystem.Systems;
using VContainer;

namespace RoboSouls.JudgeSystem.RoboMaster2026UC.Entities;

public class Sentry : RobotBase, IHealthed, IShooter, IChassisd
{
    public Sentry(Identity id)
        : base(id)
    {
    }

    [Inject] internal ICacheReader<uint> UintCacheBox { get; set; }

    [Inject] internal ICacheReader<int> IntCacheBox { get; set; }

    [Inject] internal ICacheReader<float> FloatCacheBox { get; set; }

    public byte ChassisType => 0;

    public uint Health => UintCacheBox.WithReaderNamespace(Id).Load(IHealthed.HealthCacheKey);

    public int AmmoAllowance =>
        IntCacheBox.WithReaderNamespace(Id).Load(IShooter.AmmoAllowanceCacheKey);

    public float Heat => FloatCacheBox.WithReaderNamespace(Id).Load(IShooter.HeatCacheKey);
    public byte AmmoType => PerformanceSystemBase.AmmoType17mm;
    public byte GunType => 0;
}