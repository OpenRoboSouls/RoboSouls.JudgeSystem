using System;
using System.Threading;
using System.Threading.Tasks;
using RoboSouls.JudgeSystem.Attributes;
using RoboSouls.JudgeSystem.RoboMaster2026UC.Entities;
using RoboSouls.JudgeSystem.RoboMaster2026UC.Events;
using RoboSouls.JudgeSystem.Systems;
using VContainer;
using VitalRouter;

namespace RoboSouls.JudgeSystem.RoboMaster2026UC.Systems;

/// <summary>
///     空中支援
///     每局比赛中，空中机器人拥有 750 发允许发弹量。在七分钟比赛阶段，空中机器人不能从任何途径获取弹
///     丸。
///     比赛开始时，空中机器人拥有 30 秒空中支援时间，随后每 1 分钟获得额外 20 秒空中支援时间。
///     云台手可以通过裁判系统选手端呼叫或暂停空中支援。在空中支援时间内，空中机器人将获得第一视角画
///     面，同时空中机器人与停机坪不接触时，可发射弹丸。空中支援期间，机器人发射机构解锁，反之锁定。
///     在空中支援时间耗尽后，若云台手未暂停空中支援，此后每秒额外的空中支援时间都将消耗 1 金币，详见
///     “表 5-8 兑换规则”。
///     空中机器人被雷达反制
///     雷达系统上可安装激光发射装置，用于瞄准和照射空中机器人上的激光检测模块。仅在对方空中机器人发
///     起空中支援时，己方雷达系统上的激光发射装置及其执行机构上电。空中机器人具有“被瞄准进度” P，取
///     值范围为 0（初始及最小值）至 100（最大值）。当 P 达到 100 时，空中机器人的发射机构被锁定，同
///     时 P 归零，锁定状态持续 45 秒后解除。每局比赛中，最多可通过该机制对空中机器人发射机构进行 3 次
///     锁定。112 © 2025 大疆 版权所有
///     “被瞄准进度”计算逻辑如下所示：
///     参数设定：
///      t：当前激光连续照射的时长
///      Δt：判定周期，固定为 0.1 秒
///      n：连续判定次数（照射开始时 n=0，每触发一次判定 n 加 1）
///     判定规则：
///     当空中机器人激光检测模块未被激光照射时，进度 P 以速率 0.5/s 匀速衰减，但不会小于 0，同时 t、 n
///     立即归零；
///     当空中机器人激光检测模块被激光照射时， P 停止衰减。每当 t 累计满 0.1 秒，触发一次进度增加：
///      第 1 个 0.1 秒： P=P+1
///      第 2 个 0.1 秒： P=P+2
///      第 n 个 0.1 秒： P=P+n
///     若单次持续照射时间不足 0.1 秒即中断，则 t、 n 立即归零
/// </summary>
[Routes]
public sealed partial class AerialSystem(
    EntitySystem entitySystem,
    ITimeSystem timeSystem,
    ICacheWriter<bool> boolCacheBoxWriter,
    ICacheWriter<float> floatCacheBoxWriter,
    ICacheProvider<double> doubleCache,
    ICacheProvider<int> intCache,
    ICommandPublisher publisher,
    EconomySystem economySystem,
    BattleSystem battleSystem,
    BuffSystem buffSystem,
    ILogger logger,
    Router router,
    ModuleSystem moduleSystem)
    : ISystem
{
    public const int AerialAmmoAllowance = 750;
    public const int MaxRadarCounterCount = 3;


    [Property(nameof(doubleCache))] public partial double AirStrikeStartTime { get; private set; }

    [Property(nameof(doubleCache))] public partial double AirStrikeStopTime { get; private set; }

    /// <summary>
    ///     雷达标记进度P
    /// </summary>
    [Property(nameof(doubleCache), PropertyStorageMode.Single | PropertyStorageMode.Identity)]
    public partial double RadarLockProgress { get; private set; }


    /// <summary>
    ///     受到雷达压制次数
    /// </summary>
    [Property(nameof(intCache), PropertyStorageMode.Single | PropertyStorageMode.Identity)]
    public partial int RadarCounterCount { get; private set; }

    public Task Reset(CancellationToken cancellation = new())
    {
        timeSystem.RegisterRepeatAction(
            1,
            async () =>
            {
                if (entitySystem.TryGetOperatedEntity(Identity.RedAerial, out Aerial redAerial))
                    await AirStrikeTimeSettlement(redAerial);

                if (entitySystem.TryGetOperatedEntity(Identity.BlueAerial, out Aerial blueAerial))
                    await AirStrikeTimeSettlement(blueAerial);
            }
        );

        timeSystem.RegisterRepeatAction(
            0.1,
            async () =>
            {
                if (entitySystem.TryGetOperatedEntity(Identity.RedAerial, out Aerial redAerial))
                    await AerialRadarLockSettlement(redAerial);

                if (entitySystem.TryGetOperatedEntity(Identity.BlueAerial, out Aerial blueAerial))
                    await AerialRadarLockSettlement(blueAerial);
            }
        );


        var redAerial = (Aerial)entitySystem.Entities[Identity.RedAerial];
        var blueAerial = (Aerial)entitySystem.Entities[Identity.BlueAerial];

        redAerial.AirStrikeTimeRemaining = 0;
        blueAerial.AirStrikeTimeRemaining = 0;

        // 比赛开始时，空中机器人拥有 30 秒空中支援时间
        timeSystem.RegisterOnceAction(
            JudgeSystemStage.Match,
            0,
            () =>
            {
                redAerial.AirStrikeTimeRemaining += 30;
                blueAerial.AirStrikeTimeRemaining += 30;
            }
        );

        // 随后每 1 分钟获得额外 20 秒空中支援时间
        void AddAct()
        {
            redAerial.AirStrikeTimeRemaining += 20;
            blueAerial.AirStrikeTimeRemaining += 20;
        }

        timeSystem.RegisterOnceAction(JudgeSystemStage.Match, 60, AddAct);
        timeSystem.RegisterOnceAction(JudgeSystemStage.Match, 120, AddAct);
        timeSystem.RegisterOnceAction(JudgeSystemStage.Match, 180, AddAct);
        timeSystem.RegisterOnceAction(JudgeSystemStage.Match, 240, AddAct);
        timeSystem.RegisterOnceAction(JudgeSystemStage.Match, 300, AddAct);
        timeSystem.RegisterOnceAction(JudgeSystemStage.Match, 360, AddAct);

        timeSystem.RegisterOnceAction(
            JudgeSystemStage.Match,
            0,
            () =>
            {
                battleSystem.SetAmmoAllowance(redAerial, AerialAmmoAllowance);
                battleSystem.SetAmmoAllowance(blueAerial, AerialAmmoAllowance);
                SetIsAirStriking(redAerial, false);
                SetIsAirStriking(blueAerial, false);
            }
        );

        return Task.CompletedTask;
    }

    [Inject]
    internal void Inject(Router r)
    {
        MapTo(r);
    }

    /// <summary>
    ///     呼叫空中支援
    /// </summary>
    /// <param name="camp"></param>
    public void StartAirStrike(Camp camp)
    {
        var aerialId = camp switch
        {
            Camp.Red => Identity.RedAerial,
            Camp.Blue => Identity.BlueAerial,
            _ => throw new ArgumentOutOfRangeException(nameof(camp), camp, null)
        };

        if (!entitySystem.TryGetOperatedEntity(aerialId, out Aerial aerial))
            return;

        if (TryGetIsAirStrikeCoolingDown(aerialId, out _))
            return;

        SetIsAirStriking(aerial, true);
    }

    public void StopAirStrike(Camp camp)
    {
        var aerialId = camp switch
        {
            Camp.Red => Identity.RedAerial,
            Camp.Blue => Identity.BlueAerial,
            _ => throw new ArgumentOutOfRangeException(nameof(camp), camp, null)
        };

        if (!entitySystem.TryGetOperatedEntity(aerialId, out Aerial aerial))
            return;

        SetIsAirStriking(aerial, false);
    }

    private void SetIsAirStriking(Aerial aerial, bool isAirStriking)
    {
        if (!IsRadarCountering(aerial)) moduleSystem.SetGunLocked(aerial.Id, !isAirStriking);

        if (aerial.IsAirStriking == isAirStriking)
            return;

        aerial.IsAirStriking = isAirStriking;

        if (isAirStriking)
        {
            var t = timeSystem.Time;
            SetAirStrikeStartTime(aerial.Id, t);
            publisher.PublishAsync(new AirstrikeStartEvent(aerial.Id, t));
        }
        else
        {
            var t = timeSystem.Time;
            SetAirStrikeStopTime(aerial.Id, t);
            publisher.PublishAsync(new AirstrikeStopEvent(aerial.Id, t));
        }
    }

    private static double CalculateAirStrikeCooldownTime(double lastElapsed)
    {
        return lastElapsed switch
        {
            < 0 => 0,
            >= 0 and <= 15 => 15,
            > 15 and <= 25 => 25,
            > 25 and <= 35 => 35,
            > 35 => 45
        };
    }

    public bool TryGetIsAirStrikeCoolingDown(in Identity id, out double cooldownRemaining)
    {
        var stopped = GetAirStrikeStopTime(id);
        var elapsed = stopped - GetAirStrikeStartTime(id);
        if (elapsed <= 0)
        {
            cooldownRemaining = 0;
            return false;
        }

        var cooldownRequired = CalculateAirStrikeCooldownTime(elapsed);
        var cooldownElapsed = timeSystem.Time - stopped;
        cooldownRemaining = cooldownRequired - cooldownElapsed;
        return cooldownRemaining > 0;
    }

    /// <summary>
    ///     空中支援时间结算
    /// </summary>
    /// <param name="aerial"></param>
    /// <returns></returns>
    private Task AirStrikeTimeSettlement(Aerial aerial)
    {
        if (!aerial.IsAirStriking)
            return Task.CompletedTask;

        if (
            timeSystem.Time - GetAirStrikeStartTime(aerial.Id)
            >= RM2026ucPerformanceSystem.MaxAerialStrikeTime
        )
            StopAirStrike(aerial.Id.Camp);

        if (aerial.AirStrikeTimeRemaining > 0)
        {
            aerial.AirStrikeTimeRemaining -= 1;
        }
        else
        {
            if (!economySystem.TryDecreaseCoin(aerial.Id.Camp, 1)) StopAirStrike(aerial.Id.Camp);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    ///     当前激光连续照射的时长t
    /// </summary>
    /// <param name="aerial"></param>
    /// <returns></returns>
    public double GetRadarLockDuration(Aerial aerial)
    {
        if (!buffSystem.TryGetBuff(aerial.Id, RM2026ucBuffs.RadarLock, out float startTime)) return 0;

        return timeSystem.Time - startTime;
    }

    /// <summary>
    ///     连续判定次数
    /// </summary>
    /// <param name="aerial"></param>
    /// <returns></returns>
    public int GetRadarLockCount(Aerial aerial)
    {
        var duration = GetRadarLockDuration(aerial);

        return (int)Math.Floor(duration / 0.1);
    }

    /// <summary>
    ///     是否正在被雷达压制
    /// </summary>
    /// <param name="aerial"></param>
    /// <returns></returns>
    public bool IsRadarCountering(Aerial aerial)
    {
        return buffSystem.TryGetBuff(aerial.Id, RM2026ucBuffs.RadarCountered, out Buff _);
    }

    /// <summary>
    ///     雷达反制结算
    /// </summary>
    /// <param name="aerial"></param>
    /// <returns></returns>
    private Task AerialRadarLockSettlement(Aerial aerial)
    {
        var progress = GetRadarLockProgress(aerial.Id);
        var counterCount = GetRadarCounterCount(aerial.Id);
        if (counterCount >= MaxRadarCounterCount) return Task.CompletedTask;

        if (progress >= 100)
        {
            router.PublishAsync(new AerialCounteredEvent(aerial.Id, timeSystem.Time));
            buffSystem.AddBuff(aerial.Id, RM2026ucBuffs.RadarCountered, 0, TimeSpan.FromSeconds(45));
            SetRadarCounterCount(aerial.Id, counterCount + 1);
            moduleSystem.SetGunLocked(aerial.Id, true);

            SetRadarLockProgress(aerial.Id, 0);
        }

        if (!buffSystem.TryGetBuff(aerial.Id, RM2026ucBuffs.RadarLock, out float startTime))
        {
            // 当空中机器人激光检测模块未被激光照射时，进度 P 以速率 0.5/s 匀速衰减，但不会小于 0
            progress -= 0.05;
            SetRadarLockProgress(aerial.Id, Math.Max(progress, 0));
            return Task.CompletedTask;
        }

        /*
当空中机器人激光检测模块被激光照射时， P 停止衰减。每当 t 累计满 0.1 秒，触发一次进度增加：
 第 1 个 0.1 秒： P=P+1
 第 2 个 0.1 秒： P=P+2
 第 n 个 0.1 秒： P=P+n
         */

        var lockCount = Math.Floor(timeSystem.Time - startTime);
        progress += lockCount;
        SetRadarLockProgress(aerial.Id, Math.Max(progress, 100));

        return Task.CompletedTask;
    }

    [Route]
    private void OnRadarLocked(AerialLockedEvent evt)
    {
        if (!entitySystem.TryGetOperatedEntity(evt.AerialId, out Aerial a))
        {
            logger.Warning("wtf locking unattended aerial", evt);
            return;
        }

        buffSystem.AddBuff(a.Id, RM2026ucBuffs.RadarLock, timeSystem.TimeAsFloat, TimeSpan.MaxValue);
    }

    [Route]
    private void OnRadarLockStopped(AerialLockStoppedEvent evt)
    {
        if (!entitySystem.TryGetOperatedEntity(evt.AerialId, out Aerial a))
        {
            logger.Warning("wtf locking unattended aerial", evt);
            return;
        }

        buffSystem.RemoveBuff(a.Id, RM2026ucBuffs.RadarLock);
    }
}