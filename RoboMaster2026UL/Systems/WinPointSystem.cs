using System.Threading;
using System.Threading.Tasks;
using RoboSouls.JudgeSystem.Events;
using RoboSouls.JudgeSystem.RoboMaster2026UL.Events;
using RoboSouls.JudgeSystem.Systems;
using VContainer;
using VitalRouter;

namespace RoboSouls.JudgeSystem.RoboMaster2026UL.Systems;

[Routes]
public sealed partial class WinPointSystem : OccupyZoneSystemBase
{
    private static readonly int WinPointCacheKey = "WinPoint".Sum();
    public static readonly Identity CentralZoneId = new(Camp.Judge, 150);
    private bool _bluePointDelta140Dispatched;

    private bool _bluePointDelta70Dispatched;

    /// <summary>
    ///     红方胜利点首次差140时的200金币增长
    /// </summary>
    private bool _redPointDelta140Dispatched;

    /// <summary>
    ///     红方胜利点首次差70时的200金币增长
    /// </summary>
    private bool _redPointDelta70Dispatched;

    [Inject] internal ICacheProvider<int> IntCacheBox { get; set; }

    [Inject] internal ILogger Logger { get; set; }

    [Inject] internal ITimeSystem TimeSystem { get; set; }

    [Inject] internal ICacheProvider<double> DoubleCacheBox { get; set; }

    [Inject] internal EntitySystem EntitySystem { get; set; }

    [Inject] internal ZoneSystem ZoneSystem { get; set; }

    [Inject] internal ICommandPublisher CommandPublisher { get; set; }

    [Inject] internal EconomySystem EconomySystem { get; set; }

    public override Identity ZoneId => CentralZoneId;

    [Inject]
    internal void Inject(Router router)
    {
        MapTo(router);
    }

    public override async Task Reset(CancellationToken cancellation = new())
    {
        await base.Reset(cancellation);
        TimeSystem.RegisterRepeatAction(1, CentralZoneDetectLoop);

        SetWinPoint(Camp.Red, RM2026ulPerformanceSystem.InitialWinPoint);
        SetWinPoint(Camp.Blue, RM2026ulPerformanceSystem.InitialWinPoint);
    }

    protected override void OnZoneOccupied(Camp camp)
    {
    }

    protected override void OnZoneLost(Camp camp)
    {
    }

    protected override void OnOccupierEnterZone(in Identity operatorId)
    {
    }

    protected override void OnOccupierLeaveZone(in Identity operatorId)
    {
    }

    public int GetWinPoint(Camp camp)
    {
        return IntCacheBox.WithReaderNamespace(new Identity(camp, 0))
            .LoadOrDefault(WinPointCacheKey, RM2026ulPerformanceSystem.InitialWinPoint);
    }

    private void SetWinPoint(Camp camp, int value)
    {
        IntCacheBox.WithWriterNamespace(new Identity(camp, 0)).Save(WinPointCacheKey, value);

        CommandPublisher.PublishAsync(new WinPointCommand(camp, GetWinPoint(camp)));
    }

    private Task CentralZoneDetectLoop()
    {
        if (TimeSystem.Stage != JudgeSystemStage.Match)
            return Task.CompletedTask;
        if (CurrentOccupier == Camp.Spectator)
            return Task.CompletedTask;

        SetWinPoint(CurrentOccupier.GetOppositeCamp(), GetWinPoint(CurrentOccupier) - 1);

        return Task.CompletedTask;
    }

    protected override void OnKill(KillEvent evt)
    {
        base.OnKill(evt);
        if (TimeSystem.Stage != JudgeSystemStage.Match)
            return;

        var camp = evt.Victim.Camp;
        SetWinPoint(camp, GetWinPoint(camp) - 20);
    }

    [Route]
    private void OnWinPoint(WinPointCommand evt)
    {
        var redPoint = GetWinPoint(Camp.Red);
        var bluePoint = GetWinPoint(Camp.Blue);

        if (redPoint - bluePoint >= 70 && !_bluePointDelta70Dispatched)
        {
            _bluePointDelta70Dispatched = true;
            EconomySystem.AddCoin(Camp.Blue, 200);
        }

        if (redPoint - bluePoint >= 140 && !_bluePointDelta140Dispatched)
        {
            _bluePointDelta140Dispatched = true;
            EconomySystem.AddCoin(Camp.Blue, 200);
        }

        if (bluePoint - redPoint >= 70 && !_redPointDelta70Dispatched)
        {
            _redPointDelta70Dispatched = true;
            EconomySystem.AddCoin(Camp.Red, 200);
        }

        if (bluePoint - redPoint >= 140 && !_redPointDelta140Dispatched)
        {
            _redPointDelta140Dispatched = true;
            EconomySystem.AddCoin(Camp.Red, 200);
        }
    }
}