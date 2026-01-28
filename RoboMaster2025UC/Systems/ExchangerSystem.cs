using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using RoboSouls.JudgeSystem.Events;
using RoboSouls.JudgeSystem.RoboMaster2025UC.Entities;
using RoboSouls.JudgeSystem.RoboMaster2025UC.Events;
using RoboSouls.JudgeSystem.Systems;
using VContainer;
using VitalRouter;

namespace RoboSouls.JudgeSystem.RoboMaster2025UC.Systems;

/// <summary>
///     兑换站机制
/// </summary>
[Routes]
public sealed partial class ExchangerSystem : ISystem
{
    public static readonly Identity RedExchangeZoneId = new(Camp.Red, 60);
    public static readonly Identity BlueExchangeZoneId = new(Camp.Blue, 60);

    private static readonly int ExchangerStateCacheKey = "ExchangerState".Sum();
    private static readonly int ExchangerLevelCacheKey = "ExchangerLevel".Sum();
    private static readonly int ExchangerStartTimeCacheKey = "ExchangerStartTime".Sum();
    private static readonly int ExchangerEndTimeCacheKey = "ExchangerEndTime".Sum();
    private static readonly int ExchangerSumCoinCacheKey = "ExchangerSumCoin".Sum();

    private static readonly ReadOnlyCollection<int> AvailableLevelsL1 = new List<int>
    {
        1,
        2,
        3,
        4
    }.AsReadOnly();

    private static readonly ReadOnlyCollection<int> AvailableLevelsL2 = new List<int>
    {
        2,
        3,
        4
    }.AsReadOnly();

    private static readonly ReadOnlyCollection<int> AvailableLevelsL3 = new List<int>
    {
        3,
        4
    }.AsReadOnly();

    private static readonly ReadOnlyCollection<int> AvailableLevelsL4 = new List<int>
    {
        4
    }.AsReadOnly();

    [Inject] internal ITimeSystem TimeSystem { get; set; }

    [Inject] internal LifeSystem LifeSystem { get; set; }

    [Inject] internal EntitySystem EntitySystem { get; set; }

    [Inject] internal EconomySystem EconomySystem { get; set; }

    [Inject] internal ICommandPublisher Publisher { get; set; }

    [Inject] internal ICacheProvider<byte> ByteCacheProvider { get; set; }

    [Inject] internal ICacheProvider<int> IntCacheProvider { get; set; }

    [Inject] internal ICacheProvider<double> DoubleCacheProvider { get; set; }

    [Inject] internal ILogger Logger { get; set; }

    [Inject] internal RM2025ucPerformanceSystem PerformanceSystem { get; set; }

    [Inject] internal RM2025ucOperatorSystem OperatorSystem { get; set; }

    public Task Reset(CancellationToken cancellation = new())
    {
        SetExchangerState(Camp.Red, ExchangerState.Idle);
        SetExchangerState(Camp.Blue, ExchangerState.Idle);
        return Task.CompletedTask;
    }

    [Inject]
    internal void Inject(Router router)
    {
        MapTo(router);
    }

    /// <summary>
    ///     获取兑换站当前状态
    /// </summary>
    /// <param name="camp"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public ExchangerState GetExchangerState(Camp camp)
    {
        var id = camp switch
        {
            Camp.Red => RedExchangeZoneId,
            Camp.Blue => BlueExchangeZoneId,
            _ => throw new ArgumentOutOfRangeException(nameof(camp), camp, null)
        };

        return ByteCacheProvider
            .WithReaderNamespace(id)
            .TryLoad(ExchangerStateCacheKey, out var state)
            ? (ExchangerState)state
            : ExchangerState.Idle;
    }

    private void SetExchangerState(Camp camp, ExchangerState state)
    {
        var oldState = GetExchangerState(camp);
        if (oldState == state)
            return;

        var id = camp switch
        {
            Camp.Red => RedExchangeZoneId,
            Camp.Blue => BlueExchangeZoneId,
            _ => throw new ArgumentOutOfRangeException(nameof(camp), camp, null)
        };

        ByteCacheProvider.WithWriterNamespace(id).Save(ExchangerStateCacheKey, (byte)state);

        Publisher.PublishAsync(new ExchangeStateChangeEvent(oldState, state, camp));
    }

    /// <summary>
    ///     获取当前可用的兑换难度
    ///     累计通过兑换获得的金币对当次兑换获得金币的影响
    ///     随着通过矿石兑换所获得的累计经济的增加，参赛队伍可选择的最低难度等级将逐渐被限制，但此后兑
    ///     换的每个矿石所获得的金币将乘以一定的倍率，具体机制如下：
    ///     表 5-14 累计经济与难度限制
    ///     累计金币数 难度限制 金币倍率
    ///     1075 最低选择二级 1 倍
    ///     1225 最低选择三级 1.4 倍
    ///     2000 最低选择四级 2 倍
    /// </summary>
    /// <param name="camp"></param>
    /// <returns></returns>
    public ReadOnlyCollection<int> GetAvailableLevels(Camp camp)
    {
        var sum = GetExchangedSum(camp);
        return sum switch
        {
            < 1075 => AvailableLevelsL1,
            < 1225 => AvailableLevelsL2,
            < 2000 => AvailableLevelsL3,
            _ => AvailableLevelsL4
        };
    }

    /// <summary>
    ///     当前兑换站设置等级
    /// </summary>
    /// <param name="camp"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public int GetExchangerLevel(Camp camp)
    {
        var id = camp switch
        {
            Camp.Red => RedExchangeZoneId,
            Camp.Blue => BlueExchangeZoneId,
            _ => throw new ArgumentOutOfRangeException(nameof(camp), camp, null)
        };

        return IntCacheProvider.WithReaderNamespace(id).Load(ExchangerLevelCacheKey);
    }

    private void SetExchangerLevel(Camp camp, int level)
    {
        var id = camp switch
        {
            Camp.Red => RedExchangeZoneId,
            Camp.Blue => BlueExchangeZoneId,
            _ => throw new ArgumentOutOfRangeException(nameof(camp), camp, null)
        };

        IntCacheProvider.WithWriterNamespace(id).Save(ExchangerLevelCacheKey, level);
    }

    /// <summary>
    ///     上次兑换开始时间
    /// </summary>
    /// <param name="camp"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public double GetExchangeStartTime(Camp camp)
    {
        var id = camp switch
        {
            Camp.Red => RedExchangeZoneId,
            Camp.Blue => BlueExchangeZoneId,
            _ => throw new ArgumentOutOfRangeException(nameof(camp), camp, null)
        };

        return DoubleCacheProvider
            .WithReaderNamespace(id)
            .TryLoad(ExchangerStartTimeCacheKey, out var time)
            ? time
            : 0;
    }

    private void SetExchangeStartTime(Camp camp, double time)
    {
        var id = camp switch
        {
            Camp.Red => RedExchangeZoneId,
            Camp.Blue => BlueExchangeZoneId,
            _ => throw new ArgumentOutOfRangeException(nameof(camp), camp, null)
        };

        DoubleCacheProvider.WithWriterNamespace(id).Save(ExchangerStartTimeCacheKey, time);
    }

    /// <summary>
    ///     上次兑换结束时间
    /// </summary>
    /// <param name="camp"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public double GetExchangeEndTime(Camp camp)
    {
        var id = camp switch
        {
            Camp.Red => RedExchangeZoneId,
            Camp.Blue => BlueExchangeZoneId,
            _ => throw new ArgumentOutOfRangeException(nameof(camp), camp, null)
        };

        return DoubleCacheProvider
            .WithReaderNamespace(id)
            .TryLoad(ExchangerEndTimeCacheKey, out var time)
            ? time
            : 0;
    }

    /// <summary>
    ///     获取累计兑换金币数量
    /// </summary>
    /// <param name="camp"></param>
    /// <returns></returns>
    public int GetExchangedSum(Camp camp)
    {
        var id = camp switch
        {
            Camp.Red => RedExchangeZoneId,
            Camp.Blue => BlueExchangeZoneId,
            _ => throw new ArgumentOutOfRangeException(nameof(camp), camp, null)
        };

        return IntCacheProvider
            .WithReaderNamespace(id)
            .TryLoad(ExchangerSumCoinCacheKey, out var sum)
            ? sum
            : 0;
    }

    private void SetExchangedSum(Camp camp, int sum)
    {
        var id = camp switch
        {
            Camp.Red => RedExchangeZoneId,
            Camp.Blue => BlueExchangeZoneId,
            _ => throw new ArgumentOutOfRangeException(nameof(camp), camp, null)
        };

        IntCacheProvider.WithWriterNamespace(id).Save(ExchangerSumCoinCacheKey, sum);
    }

    private void SetExchangeEndTime(Camp camp, double time)
    {
        var id = camp switch
        {
            Camp.Red => RedExchangeZoneId,
            Camp.Blue => BlueExchangeZoneId,
            _ => throw new ArgumentOutOfRangeException(nameof(camp), camp, null)
        };

        DoubleCacheProvider.WithWriterNamespace(id).Save(ExchangerEndTimeCacheKey, time);
    }

    /// <summary>
    ///     选择难度，开始兑换
    ///     由工程机器人调用
    /// </summary>
    /// <param name="camp"></param>
    /// <param name="level"></param>
    /// <returns></returns>
    public bool TryStartExchange(Camp camp, int level)
    {
        if (TimeSystem.Stage != JudgeSystemStage.Match)
            return false;
        if (level is < 1 or > 4)
            return false;
        if (GetExchangerState(camp) != ExchangerState.Idle)
            return false;
        if (!GetAvailableLevels(camp).Contains(level))
            return false;

        SetExchangerLevel(camp, level);
        SetExchangerState(camp, ExchangerState.AdjustToExchange);
        return true;
    }

    public void CancelExchange(Camp camp)
    {
        if (TimeSystem.Stage != JudgeSystemStage.Match)
            return;
        SetExchangerState(camp, ExchangerState.AdjustToReset);
    }

    /// <summary>
    ///     兑换站姿态调整完成
    ///     由兑换站调用
    /// </summary>
    /// <param name="camp"></param>
    public void OnExchangerAdjustFinished(Camp camp)
    {
        var state = GetExchangerState(camp);
        if (state == ExchangerState.AdjustToExchange)
        {
            SetExchangerState(camp, ExchangerState.Exchanging);
            SetExchangeStartTime(camp, TimeSystem.StageTimeElapsed);
        }
        else if (state == ExchangerState.AdjustToReset)
        {
            SetExchangerState(camp, ExchangerState.Idle);
        }
        else if (state == ExchangerState.AdjustToPrune)
        {
            SetExchangerState(camp, ExchangerState.Pruning);
        }
    }

    /// <summary>
    ///     兑换站确认兑换
    ///     由兑换站调用
    /// </summary>
    /// <param name="camp"></param>
    public void OnExchangerConfirmExchange(Camp camp)
    {
        if (TimeSystem.Stage != JudgeSystemStage.Match)
            return;
        var state = GetExchangerState(camp);
        if (state != ExchangerState.Exchanging)
            return;

        SetExchangerState(camp, ExchangerState.AdjustToPrune);
        SetExchangeEndTime(camp, TimeSystem.StageTimeElapsed);
    }

    /// <summary>
    ///     兑换站成功识别矿石（清矿完成）
    ///     由兑换站调用
    /// </summary>
    /// <param name="camp"></param>
    /// <param name="type"></param>
    public void OnExchangerOreDetected(Camp camp, OreType type)
    {
        if (TimeSystem.Stage != JudgeSystemStage.Match)
            return;
        var state = GetExchangerState(camp);
        if (state != ExchangerState.Pruning)
            return;
        SetExchangerState(camp, ExchangerState.AdjustToReset);
        var engineerId = camp switch
        {
            Camp.Red => Identity.RedEngineer,
            Camp.Blue => Identity.BlueEngineer,
            _ => throw new ArgumentOutOfRangeException(nameof(camp), camp, null)
        };

        var startTime = GetExchangeStartTime(camp);
        var endTime = GetExchangeEndTime(camp);

        if (startTime > endTime) throw new InvalidOperationException("Invalid exchange time");

        var level = GetExchangerLevel(camp);
        var duration = endTime - startTime;
        var multiplier = GetExchangeGainMultiplier(camp);
        var gain = (int)Math.Round(GetExchangeGain(camp, level, type, duration) * multiplier);

        if (gain <= 0)
            return;

        Publisher.PublishAsync(
            new ExchangeOreSuccessEvent(
                TimeSystem.StageTimeElapsed,
                type,
                level,
                duration,
                engineerId,
                gain,
                multiplier
            )
        );

        EconomySystem.AddCoin(camp, gain);
    }

    /// <summary>
    ///     计算兑换获得金币
    ///     定义 A 为机器人此次兑换的基础金币数， B 为机器人兑换的该类型矿石上一级别难度对应的基础金币
    ///     数， t 为兑换用时， m 为工程机器人随矿石兑换所获得的累计经济添加的倍率， n 为工程机器人控制方式
    ///     所添加的倍率，每次兑换后，机器人获得的实际金币值为：
    ///      一级难度： A*m*n
    ///      二级与三级难度：
    ///      A*m*n (t≤15)
    ///      (A-0.02*(t-15)*(A-B))*m*n， (t>15)
    ///      四级难度：
    ///      A1*m1*n+A2*m2*n (∑ t≤20)
    ///      0 (∑ t>20)
    ///     若工程机器人正处于自动兑矿操作方式，通过兑换矿石获得的经济提升 50%；若工程机器人正处于半自
    ///     动控制操作方式， 通过兑换矿石获得的经济提升 100%， 该值计算方式为独立乘算。
    /// </summary>
    /// <returns></returns>
    public int GetExchangeGain(Camp camp, int level, OreType oreType, double t)
    {
        var engineerId = camp switch
        {
            Camp.Red => Identity.RedEngineer,
            Camp.Blue => Identity.BlueEngineer,
            _ => throw new ArgumentOutOfRangeException(nameof(camp), camp, null)
        };

        var controlMode = OperatorSystem.GetControlMode(engineerId);

        var a = PerformanceSystem.GetExchangeBaseCoin(level, oreType);
        var b = level > 1 ? PerformanceSystem.GetExchangeBaseCoin(level - 1, oreType) : 0;
        var m = GetExchangeGainMultiplier(camp);
        var n = controlMode switch
        {
            ControlMode.SemiAuto => 2,
            ControlMode.AutoExchange => 1.5,
            _ => 1
        };

        if (t <= 0)
            return 0;

        switch (level)
        {
            case 1:
            case 2
                or 3 when t <= 15:
                return (int)(a * m * n);
            case 2
                or 3:
                return (int)((a - 0.02 * (t - 15) * (a - b)) * m * n);
            case 4 when t <= 20:
                return (int)(a * m * n);
            default:
                return 0;
        }
    }

    public static int GetExchangeMaxDuration(int level)
    {
        return level switch
        {
            1 => 40,
            2 or 3 => 65,
            4 => 20,
            _ => 0
        };
    }

    /// <summary>
    ///     累计通过兑换获得的金币对当次兑换获得金币的影响
    /// </summary>
    /// <param name="camp"></param>
    /// <returns></returns>
    public float GetExchangeGainMultiplier(Camp camp)
    {
        var sum = GetExchangedSum(camp);
        return sum switch
        {
            < 1075 => 1,
            < 1225 => 1.4f,
            _ => 2
        };
    }

    /*
     * 兑换区增益点机制
兑换区增益点仅能由己方工程机器人占领。
占领兑换区增益点后，工程机器人将处于无敌状态。
     */
    [Route]
    private void OnEnterExchangerZone(EnterZoneEvent evt)
    {
        if (TimeSystem.Stage != JudgeSystemStage.Match)
            return;
        if (evt.OperatorId.Camp != evt.ZoneId.Camp)
            return;
        if (
            !evt.ZoneId.IsRobotCamp()
            || evt.ZoneId.Id != RedExchangeZoneId.Id
            || !evt.OperatorId.IsEngineer()
        )
            return;
        if (!EntitySystem.TryGetOperatedEntity(evt.OperatorId, out Engineer engineer))
            return;

        LifeSystem.SetInvincible(engineer, true);
    }

    [Route]
    private void OnExitExchangerZone(ExitZoneEvent evt)
    {
        if (TimeSystem.Stage != JudgeSystemStage.Match)
            return;
        if (evt.OperatorId.Camp != evt.ZoneId.Camp)
            return;
        if (
            !evt.ZoneId.IsRobotCamp()
            || evt.ZoneId.Id != RedExchangeZoneId.Id
            || !evt.OperatorId.IsEngineer()
        )
            return;
        if (!EntitySystem.TryGetOperatedEntity(evt.OperatorId, out Engineer engineer))
            return;

        LifeSystem.SetInvincible(engineer, false);
    }
}

public readonly struct ExchangerPosition
{
    public readonly double X;
    public readonly double Y;
    public readonly double Z;
    public readonly double Theta;
    public readonly double Phi;
    public readonly double Alpha;

    public ExchangerPosition(
        double x,
        double y,
        double z,
        double theta,
        double phi,
        double alpha
    )
    {
        X = x;
        Y = y;
        Z = z;
        Theta = theta;
        Phi = phi;
        Alpha = alpha;
    }

    /// <summary>
    ///     随机生成姿态
    /// </summary>
    /// <param name="xRange"></param>
    /// <param name="yRange"></param>
    /// <param name="zRange"></param>
    /// <param name="thetaRange"></param>
    /// <param name="phiRange"></param>
    /// <param name="alphaRange"></param>
    /// <returns></returns>
    public static ExchangerPosition RandomPosition(
        (double, double) xRange,
        (double, double) yRange,
        (double, double) zRange,
        (double, double) thetaRange,
        (double, double) phiRange,
        (double, double) alphaRange
    )
    {
        ExchangerPosition pos = default;

        do
        {
            var rand = new Random();

            pos = new ExchangerPosition(
                rand.NextDouble() * (xRange.Item2 - xRange.Item1) + xRange.Item1,
                rand.NextDouble() * (yRange.Item2 - yRange.Item1) + yRange.Item1,
                rand.NextDouble() * (zRange.Item2 - zRange.Item1) + zRange.Item1,
                rand.NextDouble() * (thetaRange.Item2 - thetaRange.Item1) + thetaRange.Item1,
                rand.NextDouble() * (phiRange.Item2 - phiRange.Item1) + phiRange.Item1,
                rand.NextDouble() * (alphaRange.Item2 - alphaRange.Item1) + alphaRange.Item1
            );
        } while (!pos.PositionLegal());

        return pos;
    }

    /// <summary>
    ///     位姿满足条件:
    ///     x^2 + y^2 + (z-600)^2 <= 300^2
    /// </summary>
    /// <returns></returns>
    private bool PositionLegal()
    {
        return Math.Pow(X, 2) + Math.Pow(Y, 2) + Math.Pow(Z - 600, 2) <= Math.Pow(300, 2);
    }

    public override string ToString()
    {
        return $"X: {X}, Y: {Y}, Z: {Z}, Theta: {Theta}, Phi: {Phi}, Alpha: {Alpha}";
    }
}

public enum OreType : byte
{
    Gold,
    Silver
}

public enum ExchangerState : byte
{
    /// <summary>
    ///     空闲
    /// </summary>
    Idle,

    /// <summary>
    ///     兑换中
    /// </summary>
    Exchanging,

    /// <summary>
    ///     清矿中
    /// </summary>
    Pruning,

    /// <summary>
    ///     前往兑矿点
    /// </summary>
    AdjustToExchange,

    /// <summary>
    ///     前往复位点
    /// </summary>
    AdjustToReset,

    /// <summary>
    ///     前往清矿点
    /// </summary>
    AdjustToPrune
}

public static class ExchangerStateExtensions
{
    public static bool IsAdjusting(this ExchangerState state)
    {
        return state
            is ExchangerState.AdjustToExchange
            or ExchangerState.AdjustToReset
            or ExchangerState.AdjustToPrune;
    }
}