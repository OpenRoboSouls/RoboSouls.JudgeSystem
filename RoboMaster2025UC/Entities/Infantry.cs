using RoboSouls.JudgeSystem.Entities;
using RoboSouls.JudgeSystem.Systems;

namespace RoboSouls.JudgeSystem.RoboMaster2025UC.Entities;

public sealed class Infantry(
    Identity id,
    ICacheReader<uint> uintCacheBox,
    ICacheReader<int> intCacheBox,
    ICacheReader<byte> byteCacheBox,
    ICacheReader<float> floatCacheBox)
    : RobotBase(id), IHealthed, IShooter, IExperienced, IChassisd
{
    public byte ChassisType =>
        byteCacheBox.WithReaderNamespace(Id).Load(IChassisd.ChassisTypeCacheKey);

    public int Experience => intCacheBox.WithReaderNamespace(Id).Load(IExperienced.ExpCacheKey);

    public uint Health => uintCacheBox.WithReaderNamespace(Id).Load(IHealthed.HealthCacheKey);

    public int AmmoAllowance =>
        intCacheBox.WithReaderNamespace(Id).Load(IShooter.AmmoAllowanceCacheKey);

    public float Heat => floatCacheBox.WithReaderNamespace(Id).Load(IShooter.HeatCacheKey);
    public byte AmmoType => PerformanceSystemBase.AmmoType17mm;
    public byte GunType => byteCacheBox.WithReaderNamespace(Id).Load(IShooter.GunTypeCacheKey);
}