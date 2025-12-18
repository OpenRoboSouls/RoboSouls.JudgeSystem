using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RoboSouls.JudgeSystem.Entities;
using RoboSouls.JudgeSystem.RoboMaster2024UL.Entities;
using RoboSouls.JudgeSystem.RoboMaster2024UL.Events;
using RoboSouls.JudgeSystem.Systems;
using VContainer;
using VitalRouter;

namespace RoboSouls.JudgeSystem.RoboMaster2024UL.Systems;

/// <summary>
/// 中心增益点机制
/// </summary>
public sealed class CentralZoneSystem : ISystem
{
    public static readonly Identity CentralZoneId = new Identity(Camp.Judge, 150);

    public static readonly int BlueEnergyCacheKey = "blueEnergy".Sum();

    public static readonly int RedEnergyCacheKey = "redEnergy".Sum();

    public static readonly int NextActivationTimeCacheKey = "nextActivationTime".Sum();

    [Inject]
    internal ILogger Logger { get; set; }

    [Inject]
    internal ITimeSystem TimeSystem { get; set; }

    [Inject]
    internal ICacheProvider<int> IntCacheBox { get; set; }

    [Inject]
    internal ICacheProvider<double> DoubleCacheBox { get; set; }

    [Inject]
    internal EntitySystem EntitySystem { get; set; }

    [Inject]
    internal ZoneSystem ZoneSystem { get; set; }

    [Inject]
    internal ICommandPublisher CommandPublisher { get; set; }
    public int RedEnergy =>
        IntCacheBox.WithReaderNamespace(CentralZoneId).Load(RedEnergyCacheKey);
    public int BlueEnergy =>
        IntCacheBox.WithReaderNamespace(CentralZoneId).Load(BlueEnergyCacheKey);

    public bool Active =>
        TimeSystem.Stage == JudgeSystemStage.Match
        && TimeSystem.StageTimeElapsed >= NextActivationTime;

    public double NextActivationTime =>
        DoubleCacheBox.WithReaderNamespace(CentralZoneId).Load(NextActivationTimeCacheKey);

    public Task Reset(CancellationToken cancellation = new CancellationToken())
    {
        // “比赛开始1分钟后，中心增益点生效”
        TimeSystem.RegisterOnceAction(
            JudgeSystemStage.Match,
            0,
            () => SetNextActivationTime(60)
        );
        TimeSystem.RegisterRepeatAction(1, CentralZoneDetectLoop);

        return Task.CompletedTask;
    }

    private Task CentralZoneDetectLoop()
    {
        if (TimeSystem.Stage != JudgeSystemStage.Match)
            return Task.CompletedTask;
        if (!Active)
            return Task.CompletedTask;

        return Task.WhenAll(
            CentralZoneDetectLoopFor(Camp.Red),
            CentralZoneDetectLoopFor(Camp.Blue)
        );
    }

    internal void OnCentralZoneHit(in Identity victim, byte ammoType)
    {
        if (!ZoneSystem.IsInZone(victim, CentralZoneId))
            return;

        var energyPerHit = ammoType switch
        {
            PerformanceSystemBase.AmmoType17mm => 2,
            PerformanceSystemBase.AmmoType42mm => 20,
            _ => 0,
        };

        var camp = victim.Camp;
        var newEnergy = camp == Camp.Red ? RedEnergy - energyPerHit : BlueEnergy - energyPerHit;
        SetCampEnergy(camp, newEnergy);
    }

    /// <summary>
    ///  仅单一兵种占领中心增益点时，该方每秒可获得 10 点能量；
    ///  如有多个兵种占领中心增益点，但其中不包含哨兵机器人，则该方每秒可获得 10 点能量；
    ///  如有多个兵种占领中心增益点，且其中包含哨兵机器人，则该方每秒可获得 20 点能量。
    /// </summary>
    /// <param name="camp"></param>
    /// <returns></returns>
    private Task CentralZoneDetectLoopFor(Camp camp)
    {
        var anyRobotInZone = EntitySystem
            .GetOperatedEntities<IRobot>(camp)
            .Any(r => ZoneSystem.IsInZone(r.Id, CentralZoneId));
        if (!anyRobotInZone)
            return Task.CompletedTask;

        var sentryInZone = EntitySystem
            .GetOperatedEntities<Sentry>(camp)
            .Any(s => ZoneSystem.IsInZone(s.Id, CentralZoneId));
        var energyPerSecond = sentryInZone ? 20 : 10;

        var newEnergy =
            camp == Camp.Red ? RedEnergy + energyPerSecond : BlueEnergy + energyPerSecond;
        SetCampEnergy(camp, newEnergy);

        return Task.CompletedTask;
    }

    /// <summary>
    /// 占领增益点完成
    /// </summary>
    private void OnCentralZoneOccupied(Camp camp)
    {
        SetCampEnergy(Camp.Red, 0);
        SetCampEnergy(Camp.Blue, 0);

        CommandPublisher.PublishAsync(
            new CentralZoneOccupiedEvent { Time = TimeSystem.StageTimeElapsed, Camp = camp }
        );

        // “中心增益点失效状态持续时间为90秒”
        SetNextActivationTime(TimeSystem.StageTimeElapsed + 90);

        Logger.Info($"[CentralZone] {camp} occupied the central zone");
    }

    private Task SetNextActivationTime(double time)
    {
        Logger.Info($"[CentralZone] Next activation time: {time}");

        DoubleCacheBox
            .WithWriterNamespace(CentralZoneId)
            .Save(NextActivationTimeCacheKey, time);

        return Task.CompletedTask;
    }

    private void SetCampEnergy(Camp camp, int energy)
    {
        if (camp == Camp.Red)
        {
            IntCacheBox.WithWriterNamespace(CentralZoneId).Save(RedEnergyCacheKey, energy);
        }
        else if (camp == Camp.Blue)
        {
            IntCacheBox.WithWriterNamespace(CentralZoneId).Save(BlueEnergyCacheKey, energy);
        }

        if (energy >= 100)
        {
            OnCentralZoneOccupied(camp);
        }
    }
}