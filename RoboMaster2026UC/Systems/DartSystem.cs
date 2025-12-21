using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RoboSouls.JudgeSystem.Entities;
using RoboSouls.JudgeSystem.RoboMaster2026UC.Entities;
using RoboSouls.JudgeSystem.RoboMaster2026UC.Events;
using RoboSouls.JudgeSystem.Systems;
using VContainer;
using VitalRouter;

namespace RoboSouls.JudgeSystem.RoboMaster2026UC.Systems;

[Routes]
public sealed partial class DartSystem(
    ICacheProvider<byte> byteCacheBox,
    ICacheProvider<int> intCacheBox,
    ICacheProvider<double> doubleCacheBox,
    EntitySystem entitySystem,
    ITimeSystem timeSystem,
    ICommandPublisher commandPublisher,
    ILogger logger,
    IMatchConfigurationRM2026uc matchConfiguration,
    BuffSystem buffSystem,
    LifeSystem lifeSystem,
    PerformanceSystemBase performanceSystem,
    BaseSystem baseSystem,
    BattleSystem battleSystem)
    : ISystem
{
    [Inject]
    internal void Inject(Router router)
    {
        MapTo(router);
    }

    public static readonly Identity RedDartStationId = new Identity(Camp.Red, 12);
    public static readonly Identity BlueDartStationId = new Identity(Camp.Blue, 12);

    private static readonly int DartTargetCacheKey = "DartTarget".Sum();
    private static readonly int DartRemainingCacheKey = "DartRemaining".Sum();
    private static readonly int LastDartStationOpenTimeCacheKey =
        "LastDartStationOpenTime".Sum();
    private static readonly int DartStationOpenedCountCacheKey = "DartStationOpened".Sum();

    // client does not need to know
    private readonly DartCounter _blueDartCounter = new DartCounter();
    private readonly DartCounter _redDartCounter = new DartCounter();

    public Task Reset(CancellationToken cancellation = new CancellationToken())
    {
        SetCurrentTarget(Camp.Blue, DartTarget.Outpost);
        SetCurrentTarget(Camp.Red, DartTarget.Outpost);
        SetDartRemaining(Camp.Blue, RM2026ucPerformanceSystem.MaxDartCount);
        SetDartRemaining(Camp.Red, RM2026ucPerformanceSystem.MaxDartCount);
        SetLastDartStationOpenTime(Camp.Blue, 0);
        SetLastDartStationOpenTime(Camp.Red, 0);

        return Task.CompletedTask;
    }

    /// <summary>
    /// 当前飞镖目标
    /// </summary>
    /// <param name="camp"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public DartTarget GetCurrentTarget(Camp camp)
    {
        var outpostId = camp switch
        {
            Camp.Blue => Identity.RedOutpost,
            Camp.Red => Identity.BlueOutpost,
            _ => throw new ArgumentOutOfRangeException(),
        };

        var dartStationId = camp switch
        {
            Camp.Blue => BlueDartStationId,
            Camp.Red => RedDartStationId,
            _ => throw new ArgumentOutOfRangeException(),
        };

        if (entitySystem.TryGetEntity<Outpost>(outpostId, out var outpost) && outpost.IsDead())
        {
            return byteCacheBox
                .WithReaderNamespace(dartStationId)
                .TryLoad(DartTargetCacheKey, out var value)
                ? (DartTarget)value
                : DartTarget.Fixed;
        }

        return DartTarget.Outpost;
    }

    /// <summary>
    /// 是否可以切换飞镖目标
    /// </summary>
    /// <param name="camp"></param>
    /// <returns></returns>
    public bool CanSwitchDartTarget(Camp camp)
    {
        return timeSystem.Stage == JudgeSystemStage.Match
               && timeSystem.StageTimeElapsed - GetLastDartStationOpenTime(camp) > 7 + 20 + 15
               && GetDartRemaining(camp) > 0;
    }

    public double GetTimeBeforeDartStationClose(Camp camp)
    {
        var t = 7 + 20 - (timeSystem.StageTimeElapsed - GetLastDartStationOpenTime(camp));
        return Math.Max(t, 0);
    }

    public DartStationStatus GetStatus(Camp camp)
    {
        if (timeSystem.Stage != JudgeSystemStage.Match)
            return DartStationStatus.CoolingDown;
        var lastOpenTime = GetLastDartStationOpenTime(camp);
        var duration = timeSystem.StageTimeElapsed - lastOpenTime;
        switch (duration)
        {
            case < 7:
                return DartStationStatus.Opening;
            case < 7 + 20:
                return DartStationStatus.Launching;
            case < 7 + 20 + 5:
                return DartStationStatus.Closing;
            case < 7 + 20 + 5 + 15:
                return DartStationStatus.CoolingDown;
        }

        if (GetDartStationOpenCountRemaining(camp) <= 0)
            return DartStationStatus.CoolingDown;

        return DartStationStatus.Idle;
    }

    /// <summary>
    /// 切换当前飞镖目标
    /// </summary>
    /// <param name="camp"></param>
    public void SwitchCurrentTarget(Camp camp)
    {
        if (!CanSwitchDartTarget(camp))
            return;

        var outpostId = camp switch
        {
            Camp.Blue => Identity.RedOutpost,
            Camp.Red => Identity.BlueOutpost,
            _ => throw new ArgumentOutOfRangeException(),
        };

        if (entitySystem.TryGetEntity<Outpost>(outpostId, out var outpost) && outpost.IsDead())
        {
            var newTarget = GetCurrentTarget(camp) switch
            {
                DartTarget.Outpost => DartTarget.Fixed,
                DartTarget.Fixed => DartTarget.RandomFixed,
                DartTarget.RandomFixed => DartTarget.RandomMoving,
                DartTarget.RandomMoving => DartTarget.Fixed,
            };

            SetCurrentTarget(camp, newTarget);
        }
        else
        {
            SetCurrentTarget(camp, DartTarget.Outpost);
        }
    }

    /// <summary>
    /// 设置当前飞镖目标
    /// </summary>
    /// <param name="camp"></param>
    /// <param name="target"></param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    private void SetCurrentTarget(Camp camp, DartTarget target)
    {
        if (GetCurrentTarget(camp) == target)
        {
            return;
        }

        var dartStationId = camp switch
        {
            Camp.Blue => BlueDartStationId,
            Camp.Red => RedDartStationId,
            _ => throw new ArgumentOutOfRangeException(),
        };

        byteCacheBox.WithWriterNamespace(dartStationId).Save(DartTargetCacheKey, (byte)target);

        logger.Info($"{camp} switched dart target to {target}");
    }

    /// <summary>
    /// 剩余飞镖数量
    /// </summary>
    /// <param name="camp"></param>
    /// <returns></returns>
    public int GetDartRemaining(Camp camp)
    {
        var dartStationId = camp switch
        {
            Camp.Blue => BlueDartStationId,
            Camp.Red => RedDartStationId,
            _ => throw new ArgumentOutOfRangeException(),
        };
        return intCacheBox
            .WithReaderNamespace(dartStationId)
            .TryLoad(DartRemainingCacheKey, out var value)
            ? value
            : 0;
    }

    private void SetDartRemaining(Camp camp, int value)
    {
        var dartStationId = camp switch
        {
            Camp.Blue => BlueDartStationId,
            Camp.Red => RedDartStationId,
            _ => throw new ArgumentOutOfRangeException(),
        };
        intCacheBox.WithWriterNamespace(dartStationId).Save(DartRemainingCacheKey, value);
    }

    /// <summary>
    /// 上次打开飞镖舱门时间
    /// </summary>
    /// <param name="camp"></param>
    /// <returns></returns>
    public double GetLastDartStationOpenTime(Camp camp)
    {
        var dartStationId = camp switch
        {
            Camp.Blue => BlueDartStationId,
            Camp.Red => RedDartStationId,
            _ => throw new ArgumentOutOfRangeException(),
        };
        var v = doubleCacheBox
            .WithReaderNamespace(dartStationId)
            .Load(LastDartStationOpenTimeCacheKey);
        if (v <= 0)
        {
            v = double.MinValue;
        }

        return v;
    }

    private void SetLastDartStationOpenTime(Camp camp, double value)
    {
        var dartStationId = camp switch
        {
            Camp.Blue => BlueDartStationId,
            Camp.Red => RedDartStationId,
            _ => throw new ArgumentOutOfRangeException(),
        };
        doubleCacheBox
            .WithWriterNamespace(dartStationId)
            .Save(LastDartStationOpenTimeCacheKey, value);
    }

    public int GetDartStationOpenedCount(Camp camp)
    {
        var id = camp switch
        {
            Camp.Red => RedDartStationId,
            Camp.Blue => BlueDartStationId,
            _ => throw new ArgumentOutOfRangeException(),
        };

        return intCacheBox.WithReaderNamespace(id).Load(DartStationOpenedCountCacheKey);
    }

    private void SetDartStationOpenedCount(Camp camp, int value)
    {
        var id = camp switch
        {
            Camp.Red => RedDartStationId,
            Camp.Blue => BlueDartStationId,
            _ => throw new ArgumentOutOfRangeException(),
        };

        intCacheBox.WithWriterNamespace(id).Save(DartStationOpenedCountCacheKey, value);
    }

    /// <summary>
    /// 飞镖发射站闸门有 2 次开启机会，比赛开始 30 秒后和比赛开始4 分钟后各 1 次
    /// </summary>
    /// <returns></returns>
    public int GetDartStationOpenCountAllowed()
    {
        if (timeSystem.Stage is not JudgeSystemStage.Match)
            return 0;

        if (timeSystem.StageTimeElapsed > 240)
            return 2;
        else if (timeSystem.StageTimeElapsed > 30)
            return 1;

        return 0;
    }

    public double GetTimeBeforeNextOpenCountAllowance()
    {
        if (timeSystem.Stage is not JudgeSystemStage.Match)
            return 0;

        if (timeSystem.StageTimeElapsed > 30)
            return Math.Max(0, 240 - timeSystem.StageTimeElapsed);

        return Math.Max(0, 30 - timeSystem.StageTimeElapsed);
    }

    public double GetTimeBeforeDartStationCooldownComplete(Camp camp)
    {
        var duration = timeSystem.StageTimeElapsed - GetLastDartStationOpenTime(camp);

        return Math.Max(0, 7 + 20 + 5 + 15 - duration);
    }

    /// <summary>
    /// 飞镖舱门剩余开启次数
    /// </summary>
    /// <param name="camp"></param>
    /// <returns></returns>
    public int GetDartStationOpenCountRemaining(Camp camp)
    {
        return Math.Max(0, GetDartStationOpenCountAllowed() - GetDartStationOpenedCount(camp));
    }

    /// <summary>
    /// 尝试打开飞镖舱门
    /// </summary>
    /// <param name="camp"></param>
    /// <returns></returns>
    public bool TryOpenDartStation(Camp camp)
    {
        // 闸门完全开启耗时约 7 秒
        // 当发射站闸门完全开启后，云台手可通过控制飞镖系统发射飞镖，时长为 20 秒
        // 当发射站第一次关闭闸门后，飞镖发射站将会进入 15 秒的冷却期。冷却期结束后，方可第二次开启闸门。 冷却期间，飞镖发射站闸门不可开启
        if (timeSystem.Stage != JudgeSystemStage.Match)
            return false;
        if (GetDartStationOpenCountRemaining(camp) <= 0)
            return false;
        var lastOpenTime = GetLastDartStationOpenTime(camp);
        if (timeSystem.StageTimeElapsed - lastOpenTime < 7 + 20 + 15)
            return false;

        SetLastDartStationOpenTime(camp, timeSystem.StageTimeElapsed);
        SetDartStationOpenedCount(camp, GetDartStationOpenedCount(camp) + 1);

        var target = GetCurrentTarget(camp);
        logger.Info(
            $"{camp} opened dart station, remaining count: {GetDartStationOpenCountRemaining(camp)}, target: {target}"
        );

        commandPublisher.PublishAsync(new DartStationOpenEvent(camp, target));

        var now = timeSystem.StageTimeElapsed;
        timeSystem.RegisterOnceAction(
            JudgeSystemStage.Match,
            now + 7 + 20,
            () =>
            {
                logger.Info($"{camp} closed dart station");
                commandPublisher.PublishAsync(new DartStationCloseEvent(camp));
            }
        );

        return true;
    }

    private ushort _lastDartId = 0;
    private float _lastFireTime = 0;

    /// <summary>
    /// 尝试发射飞镖
    /// </summary>
    /// <param name="camp"></param>
    /// <returns></returns>
    public bool TryLaunchDart(Camp camp)
    {
        if (timeSystem.Stage != JudgeSystemStage.Match)
            return false;
        if (GetDartRemaining(camp) <= 0)
            return false;
        var lastOpenTime = GetLastDartStationOpenTime(camp);
        if (timeSystem.StageTimeElapsed - lastOpenTime < 7)
            return false;
        if (timeSystem.StageTimeElapsed - lastOpenTime > 7 + 20)
            return false;
        if (
            timeSystem.StageTimeElapsed - _lastFireTime
            < RM2026ucPerformanceSystem.DartFireInterval
        )
            return false;

        var target = GetCurrentTarget(camp);
        var dartIndex = RM2026ucPerformanceSystem.MaxDartCount - GetDartRemaining(camp);
        var hitRate = matchConfiguration.GetDartHitRate(camp);
        var hit = matchConfiguration.Random.NextDouble() < hitRate;

        logger.Info($"{camp} launched dart, target: {target}, hit rate: {hitRate}, hit: {hit}");

        var dartId = new Identity(camp, _lastDartId);
        _lastDartId++;
        commandPublisher.PublishAsync(new DartLaunchEvent(target, dartId, timeSystem.Time));
        SetDartRemaining(camp, GetDartRemaining(camp) - 1);

        if (!hit)
            return true;
        var now = timeSystem.StageTimeElapsed;
        timeSystem.RegisterOnceAction(
            JudgeSystemStage.Match,
            now + RM2026ucPerformanceSystem.DartFlyDuration,
            () =>
            {
                if (_interceptedDarts.TryGetValue(dartId, out var intercepted) && intercepted)
                {
                    logger.Info($"{camp} dart intercepted");
                    return;
                }

                logger.Info($"{camp} dart hit target");
                commandPublisher.PublishAsync(new DartHitEvent(camp, target));
            }
        );

        return true;
    }

    private readonly Dictionary<Identity, bool> _interceptedDarts =
        new Dictionary<Identity, bool>();

    public void OnDartIntercepted(Identity dartId)
    {
        _interceptedDarts[dartId] = true;
        logger.Info($"Dart {dartId} intercepted");
    }

    /// <summary>
    /// 飞镖命中收益
    ///  当飞镖首次、第二次、第三次、第四次命中对方前哨站、基地的“固定目标”、“随机固定目标”
    /// 时，对方所有操作手操作界面分别被遮挡 10 秒（首次）、 5 秒（第二次） 、 3 秒（第三次）、 2 秒
    /// （第四次）。当飞镖命中对方基地时，己方存活的英雄、步兵机器人分别平分 200（固定目标）、
    /// 600（随机固定目标）点经验。
    ///  当飞镖命中对方基地“随机移动目标” 时，对方所有操作手操作界面被遮挡 15 秒，且己方存活的英
    ///     雄、步兵机器人平分 2500 点经验。
    ///  若选择“随机固定目标”、 “随机移动目标”后命中基地飞镖检测模块，除去本身对基地造成的伤
    ///     害外，对方全部存活的地面机器人分别立即扣除各自当前上限血量 10%、 25%的血量（计入对方全
    ///     队总伤害，但该扣血本身和其导致的机器人战亡不生成经验）。若连续命中，则操作界面被遮挡时
    ///     间叠加计算。每次命中后检测窗口关闭 2 秒。
    ///  若选择“固定目标”、“随机固定目标”后四发飞镖全部命中基地，或选择“随机移动目标”后任
    ///     意一发飞镖命中基地，对方基地护甲将立刻展开。
    ///  当基地或前哨站的飞镖引导灯亮起时，若飞镖命中基地或者前哨站， 其对应的增益点暂时失效，持续
    ///     时间为 30 秒，若连续命中，则刷新失效时间。
    /// </summary>
    /// <param name="e"></param>
    [Route]
    private async Task OnDartHit(DartHitEvent e)
    {
        var counter = e.Camp == Camp.Blue ? ref _blueDartCounter : ref _redDartCounter;
        switch (e.Target)
        {
            case DartTarget.Outpost:
                counter.HitCountOutpost++;
                break;
            case DartTarget.Fixed:
                counter.HitCountFixed++;
                break;
            case DartTarget.RandomFixed:
                counter.HitCountRandomFixed++;
                break;
            case DartTarget.RandomMoving:
                counter.HitCountRandomMoving++;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        // dart hit buff
        var dartHitBuffDuration = 0;
        if (e.Target == DartTarget.RandomMoving)
        {
            dartHitBuffDuration = 15;
        }
        else
        {
            var sum =
                counter.HitCountOutpost + counter.HitCountFixed + counter.HitCountRandomFixed;
            dartHitBuffDuration = sum switch
            {
                1 => 10,
                2 => 5,
                3 => 3,
                4 => 2,
                _ => throw new ArgumentOutOfRangeException(),
            };
        }

        await Task.WhenAll(
            entitySystem
                .GetOperatedEntities<IRobot>(e.Camp.GetOppositeCamp())
                .Select(r =>
                {
                    var duration = dartHitBuffDuration;

                    if (
                        buffSystem.TryGetBuff(
                            r.Id,
                            RM2026ucBuffs.DartHitBuff,
                            out Buff existing
                        )
                    )
                    {
                        duration += (int)(existing.EndTime - timeSystem.Time);
                    }

                    buffSystem.AddBuff(
                        r.Id,
                        RM2026ucBuffs.DartHitBuff,
                        1,
                        TimeSpan.FromSeconds(duration)
                    );
                    return Task.CompletedTask;
                })
        );

        var dartStationId = e.Camp switch
        {
            Camp.Blue => BlueDartStationId,
            Camp.Red => RedDartStationId,
            _ => throw new ArgumentOutOfRangeException(),
        };

        // do damage
        if (e.Target == DartTarget.RandomFixed)
        {
            await Task.WhenAll(
                entitySystem
                    .GetOperatedEntities<IHealthed>(e.Camp)
                    .Select(r =>
                    {
                        var damage = (uint)(performanceSystem.GetMaxHealth(r) * 0.1);
                        lifeSystem.DecreaseHealth(r, dartStationId, damage);

                        return Task.CompletedTask;
                    })
            );
        }
        else if (e.Target == DartTarget.RandomMoving)
        {
            await Task.WhenAll(
                entitySystem
                    .GetOperatedEntities<IHealthed>(e.Camp)
                    .Select(r =>
                    {
                        var damage = (uint)(performanceSystem.GetMaxHealth(r) * 0.25);
                        lifeSystem.DecreaseHealth(r, dartStationId, damage);
                        battleSystem.AddDamageSum(e.Camp, damage);

                        return Task.CompletedTask;
                    })
            );
        }

        // base armor
        if (
            counter.HitCountFixed + counter.HitCountRandomFixed >= 4
            || counter.HitCountRandomMoving >= 1
        )
        {
            var baseId = e.Camp == Camp.Blue ? Identity.RedBase : Identity.BlueBase;
            var @base = entitySystem.Entities[baseId] as Base;
            baseSystem.SetArmorOpen(@base, true);
        }
    }
}

public enum DartTarget : byte
{
    /// <summary>
    /// 前哨站
    /// </summary>
    Outpost,

    /// <summary>
    /// 固定目标
    /// </summary>
    Fixed,

    /// <summary>
    /// 随机固定目标
    /// </summary>
    RandomFixed,

    /// <summary>
    /// 随机移动目标
    /// </summary>
    RandomMoving,
        
    /// <summary>
    /// 末端移动目标
    /// </summary>
    EndMoving,
}

public enum DartStationStatus
{
    Idle,

    CoolingDown,

    Opening,

    Closing,

    Launching,
}

internal struct DartCounter
{
    public int HitCountOutpost;
    public int HitCountFixed;
    public int HitCountRandomFixed;
    public int HitCountRandomMoving;
}