using RoboSouls.JudgeSystem.Entities;
using RoboSouls.JudgeSystem.Systems;
using VContainer;

namespace RoboSouls.JudgeSystem.RoboMaster2026UL.Entities;

public class Infantry : RobotBase, IHealthed, IShooter, IExperienced, IChassisd
{
    private byte _chassisType;

    public Infantry(Identity id)
        : base(id) { }

    [Inject]
    internal ICacheReader<uint> UintCacheBox { get; set; }

    [Inject]
    internal ICacheReader<int> IntCacheBox { get; set; }

    [Inject]
    internal ICacheReader<byte> ByteCacheBox { get; set; }

    [Inject]
    internal ICacheReader<float> FloatCacheBox { get; set; }
    public byte ChassisType =>
        ByteCacheBox.WithReaderNamespace(Id).Load(IChassisd.ChassisTypeCacheKey);
    public int Experience => IntCacheBox.WithReaderNamespace(Id).Load(IExperienced.ExpCacheKey);

    public uint Health => UintCacheBox.WithReaderNamespace(Id).Load(IHealthed.HealthCacheKey);
    public int AmmoAllowance =>
        IntCacheBox.WithReaderNamespace(Id).Load(IShooter.AmmoAllowanceCacheKey);
    public float Heat => FloatCacheBox.WithReaderNamespace(Id).Load(IShooter.HeatCacheKey);
    public byte AmmoType => PerformanceSystemBase.AmmoType17mm;
    public byte GunType => ByteCacheBox.WithReaderNamespace(Id).Load(IShooter.GunTypeCacheKey);
}