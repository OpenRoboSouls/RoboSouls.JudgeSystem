using RoboSouls.JudgeSystem.Attributes;
using RoboSouls.JudgeSystem.Entities;
using RoboSouls.JudgeSystem.Systems;
using VContainer;

namespace RoboSouls.JudgeSystem.RoboMaster2026UC.Entities;

public partial class Aerial : RobotBase, IShooter, IExperienced
{
    public Aerial(Identity id)
        : base(id) { }

    [Inject]
    internal ICacheReader<uint> UintCacheBox { get; set; }

    [Inject]
    internal ICacheReader<int> IntCacheBox { get; set; }

    [Inject]
    internal ICacheProvider<float> FloatCacheBox { get; set; }

    [Inject]
    internal ICacheProvider<bool> BoolCacheBox { get; set; }
    
    /// <summary>
    /// 空中支援剩余时间
    /// </summary>
    [Property(nameof(FloatCacheBox), PropertyStorageMode.Single, nameof(Id))]
    public partial float AirStrikeTimeRemaining
    {
        get;
        internal set;
    }

    /// <summary>
    /// 是否正在空中支援
    /// </summary>
    [Property(nameof(BoolCacheBox), PropertyStorageMode.Single, nameof(Id))]
    public partial bool IsAirStriking
    {
        get;
        internal set;
    }

    public int AmmoAllowance =>
        IntCacheBox.WithReaderNamespace(Id).Load(IShooter.AmmoAllowanceCacheKey);
    public float Heat => FloatCacheBox.WithReaderNamespace(Id).Load(IShooter.HeatCacheKey);
    public byte AmmoType => PerformanceSystemBase.AmmoType17mm;
    public byte GunType => 0;
    public int Experience => IntCacheBox.WithReaderNamespace(Id).Load(IExperienced.ExpCacheKey);
}