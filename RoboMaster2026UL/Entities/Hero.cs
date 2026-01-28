using RoboSouls.JudgeSystem.Entities;
using RoboSouls.JudgeSystem.Systems;
using VContainer;

namespace RoboSouls.JudgeSystem.RoboMaster2026UL.Entities;

public class Hero : RobotBase, IHealthed, IShooter, IChassisd
{
    public Hero(Identity id)
        : base(id)
    {
    }

    [Inject] internal ICacheReader<uint> UintCacheBox { get; set; }

    [Inject] internal ICacheReader<int> IntCacheBox { get; set; }

    [Inject] internal ICacheReader<float> FloatCacheBox { get; set; }

    public byte ChassisType => PerformanceSystemBase.ChassisTypeHero;
    public uint Health => UintCacheBox.WithReaderNamespace(Id).Load(IHealthed.HealthCacheKey);

    public int AmmoAllowance =>
        IntCacheBox.WithReaderNamespace(Id).Load(IShooter.AmmoAllowanceCacheKey);

    public float Heat => FloatCacheBox.WithReaderNamespace(Id).Load(IShooter.HeatCacheKey);
    public byte AmmoType => PerformanceSystemBase.AmmoType42mm;
    public byte GunType => PerformanceSystemBase.GunType42mmDefault;
}