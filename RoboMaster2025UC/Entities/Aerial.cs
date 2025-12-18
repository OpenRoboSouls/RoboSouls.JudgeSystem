using RoboSouls.JudgeSystem.Entities;
using RoboSouls.JudgeSystem.Systems;
using VContainer;

namespace RoboSouls.JudgeSystem.RoboMaster2025UC.Entities;

public class Aerial : RobotBase, IShooter
{
    public static readonly int AirStrikeTimeRemainingCacheKey = "AirStrikeTimeRemaining".Sum();
    public static readonly int IsAirStrikingCacheKey = "IsAirStriking".Sum();

    public Aerial(Identity id)
        : base(id) { }

    [Inject]
    internal ICacheReader<uint> UintCacheBox { get; set; }

    [Inject]
    internal ICacheReader<int> IntCacheBox { get; set; }

    [Inject]
    internal ICacheReader<float> FloatCacheBox { get; set; }

    [Inject]
    internal ICacheReader<bool> BoolCacheBox { get; set; }

    /// <summary>
    /// 空中支援剩余时间
    /// </summary>
    public float AirStrikeTimeRemaining =>
        FloatCacheBox.WithReaderNamespace(Id).Load(AirStrikeTimeRemainingCacheKey);

    /// <summary>
    /// 是否正在空中支援
    /// </summary>
    public bool IsAirStriking =>
        BoolCacheBox.WithReaderNamespace(Id).Load(IsAirStrikingCacheKey);

    public int AmmoAllowance =>
        IntCacheBox.WithReaderNamespace(Id).Load(IShooter.AmmoAllowanceCacheKey);
    public float Heat => FloatCacheBox.WithReaderNamespace(Id).Load(IShooter.HeatCacheKey);
    public byte AmmoType => PerformanceSystemBase.AmmoType17mm;
    public byte GunType => 0;
}