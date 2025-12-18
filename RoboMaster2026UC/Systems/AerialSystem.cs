using System;
using System.Threading;
using System.Threading.Tasks;
using RoboSouls.JudgeSystem.RoboMaster2026UC.Entities;
using RoboSouls.JudgeSystem.RoboMaster2026UC.Events;
using RoboSouls.JudgeSystem.Systems;
using VContainer;
using VitalRouter;

namespace RoboSouls.JudgeSystem.RoboMaster2026UC.Systems;

/// <summary>
/// 空中机器人机制
///
/// 比赛开始时，空中机器人拥有 30 秒空中支援时间，随后每 1 分钟获得额外 20 秒空中支援时间。
/// 云台手可以通过裁判系统选手端呼叫或暂停空中支援。 在空中支援时间内，空中机器人将获得第一视角画
/// 面，同时空中机器人与停机坪不接触时，可发射弹丸。在七分钟比赛阶段，空中机器人不能从任何途径获
/// 取弹丸。 空中支援期间，机器人发射机构解锁，反之锁定。
/// 在空中支援时间耗尽后，若云台手未暂停空中支援，此后每秒额外的空中支援时间都将消耗 1 金币， 详见
/// “表 5-8 兑换规则” 。
/// 每局比赛中，空中机器人拥有 1500 发允许发弹量
/// </summary>
public sealed class AerialSystem : ISystem
{
    public const int AerialAmmoAllowance = 1500;

    [Inject]
    internal EntitySystem EntitySystem { get; set; }

    [Inject]
    internal ITimeSystem TimeSystem { get; set; }

    [Inject]
    internal ICacheWriter<bool> BoolCacheBoxWriter { get; set; }

    [Inject]
    internal ICacheWriter<float> FloatCacheBoxWriter { get; set; }

    [Inject]
    internal ICacheProvider<double> DoubleCache { get; set; }

    [Inject]
    internal ICommandPublisher Publisher { get; set; }

    [Inject]
    internal EconomySystem EconomySystem { get; set; }

    [Inject]
    internal BattleSystem BattleSystem { get; set; }

    [Inject]
    internal ModuleSystem ModuleSystem { get; set; }

    public Task Reset(CancellationToken cancellation = new CancellationToken())
    {
        TimeSystem.RegisterRepeatAction(
            1,
            () =>
            {
                if (EntitySystem.TryGetOperatedEntity(Identity.RedAerial, out Aerial redAerial))
                {
                    return AirStrikeTimeSettlement(redAerial);
                }

                return Task.CompletedTask;
            }
        );

        TimeSystem.RegisterRepeatAction(
            1,
            () =>
            {
                if (
                    EntitySystem.TryGetOperatedEntity(
                        Identity.BlueAerial,
                        out Aerial blueAerial
                    )
                )
                {
                    return AirStrikeTimeSettlement(blueAerial);
                }

                return Task.CompletedTask;
            }
        );

        SetAirStrikeTime(EntitySystem.Entities[Identity.RedAerial] as Aerial, 0);
        SetAirStrikeTime(EntitySystem.Entities[Identity.BlueAerial] as Aerial, 0);

        TimeSystem.RegisterOnceAction(
            JudgeSystemStage.Match,
            0,
            () =>
            {
                AddAirStrikeTime(EntitySystem.Entities[Identity.RedAerial] as Aerial, 30);
                AddAirStrikeTime(EntitySystem.Entities[Identity.BlueAerial] as Aerial, 30);
            }
        );

        void AddAct()
        {
            AddAirStrikeTime(EntitySystem.Entities[Identity.RedAerial] as Aerial, 20);
            AddAirStrikeTime(EntitySystem.Entities[Identity.BlueAerial] as Aerial, 20);
        }

        TimeSystem.RegisterOnceAction(JudgeSystemStage.Match, 60, AddAct);
        TimeSystem.RegisterOnceAction(JudgeSystemStage.Match, 120, AddAct);
        TimeSystem.RegisterOnceAction(JudgeSystemStage.Match, 180, AddAct);
        TimeSystem.RegisterOnceAction(JudgeSystemStage.Match, 240, AddAct);
        TimeSystem.RegisterOnceAction(JudgeSystemStage.Match, 300, AddAct);
        TimeSystem.RegisterOnceAction(JudgeSystemStage.Match, 360, AddAct);

        TimeSystem.RegisterOnceAction(
            JudgeSystemStage.Match,
            0,
            () =>
            {
                BattleSystem.SetAmmoAllowance(
                    EntitySystem.Entities[Identity.RedAerial] as Aerial,
                    AerialAmmoAllowance
                );
                BattleSystem.SetAmmoAllowance(
                    EntitySystem.Entities[Identity.BlueAerial] as Aerial,
                    AerialAmmoAllowance
                );
                SetIsAirStriking(EntitySystem.Entities[Identity.RedAerial] as Aerial, false);
                SetIsAirStriking(EntitySystem.Entities[Identity.BlueAerial] as Aerial, false);
            }
        );

        return Task.CompletedTask;
    }

    /// <summary>
    /// 呼叫空中支援
    /// </summary>
    /// <param name="camp"></param>
    public void StartAirStrike(Camp camp)
    {
        var aerialId = camp switch
        {
            Camp.Red => Identity.RedAerial,
            Camp.Blue => Identity.BlueAerial,
            _ => throw new ArgumentOutOfRangeException(nameof(camp), camp, null),
        };

        if (!EntitySystem.TryGetOperatedEntity(aerialId, out Aerial aerial))
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
            _ => throw new ArgumentOutOfRangeException(nameof(camp), camp, null),
        };

        if (!EntitySystem.TryGetOperatedEntity(aerialId, out Aerial aerial))
            return;

        SetIsAirStriking(aerial, false);
    }

    private void SetIsAirStriking(Aerial aerial, bool isAirStriking)
    {
        ModuleSystem.SetGunLocked(aerial.Id, !isAirStriking);

        if (aerial.IsAirStriking == isAirStriking)
            return;

        BoolCacheBoxWriter
            .WithWriterNamespace(aerial.Id)
            .Save(Aerial.IsAirStrikingCacheKey, isAirStriking);

        if (isAirStriking)
        {
            var t = TimeSystem.Time;
            SetAirStrikeStartTime(aerial.Id, t);
            Publisher.PublishAsync(new AirstrikeStartEvent(aerial.Id, t));
        }
        else
        {
            var t = TimeSystem.Time;
            SetAirStrikeStopTime(aerial.Id, t);
            Publisher.PublishAsync(new AirstrikeStopEvent(aerial.Id, t));
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
            > 35 => 45,
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
        var cooldownElapsed = TimeSystem.Time - stopped;
        cooldownRemaining = cooldownRequired - cooldownElapsed;
        return cooldownRemaining > 0;
    }

    private static readonly int AirstrikeStartTimeCacheKey = "airstrike_start_time".Sum();
    private static readonly int AirstrikeStopTimeCacheKey = "airstrike_stop_time".Sum();

    public double GetAirStrikeStartTime(in Identity id)
    {
        return DoubleCache.WithReaderNamespace(id).Load(AirstrikeStartTimeCacheKey);
    }

    private void SetAirStrikeStartTime(in Identity id, double time)
    {
        DoubleCache.WithWriterNamespace(id).Save(AirstrikeStartTimeCacheKey, time);
    }

    private void AddAirStrikeTime(Aerial aerial, float time)
    {
        var airStrikeTimeRemaining = aerial.AirStrikeTimeRemaining + time;
        SetAirStrikeTime(aerial, airStrikeTimeRemaining);
    }

    private void SetAirStrikeTime(Aerial aerial, float time)
    {
        FloatCacheBoxWriter
            .WithWriterNamespace(aerial.Id)
            .Save(Aerial.AirStrikeTimeRemainingCacheKey, time);
    }

    private void SetAirStrikeStopTime(in Identity id, double time)
    {
        DoubleCache.WithWriterNamespace(id).Save(AirstrikeStopTimeCacheKey, time);
    }

    public double GetAirStrikeStopTime(in Identity id)
    {
        return DoubleCache.WithReaderNamespace(id).Load(AirstrikeStopTimeCacheKey);
    }

    /// <summary>
    /// 空中支援时间结算
    /// </summary>
    /// <param name="aerial"></param>
    /// <returns></returns>
    private Task AirStrikeTimeSettlement(Aerial aerial)
    {
        if (!aerial.IsAirStriking)
            return Task.CompletedTask;

        if (
            TimeSystem.Time - GetAirStrikeStartTime(aerial.Id)
            >= RM2026ucPerformanceSystem.MaxAerialStrikeTime
        )
        {
            StopAirStrike(aerial.Id.Camp);
        }

        if (aerial.AirStrikeTimeRemaining > 0)
        {
            AddAirStrikeTime(aerial, -1);
        }
        else
        {
            if (aerial.Id.Camp == Camp.Red)
            {
                if (EconomySystem.RedCoin > 1)
                {
                    EconomySystem.RedCoin -= 1;
                }
                else
                {
                    StopAirStrike(aerial.Id.Camp);
                }
            }
            else if (aerial.Id.Camp == Camp.Blue)
            {
                if (EconomySystem.BlueCoin > 1)
                {
                    EconomySystem.BlueCoin -= 1;
                }
                else
                {
                    StopAirStrike(aerial.Id.Camp);
                }
            }
        }

        return Task.CompletedTask;
    }
}