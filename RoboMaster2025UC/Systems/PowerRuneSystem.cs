using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RoboSouls.JudgeSystem.Entities;
using RoboSouls.JudgeSystem.RoboMaster2025UC.Entities;
using RoboSouls.JudgeSystem.RoboMaster2025UC.Events;
using RoboSouls.JudgeSystem.Systems;
using VContainer;
using VitalRouter;
using Random = System.Random;

namespace RoboSouls.JudgeSystem.RoboMaster2025UC.Systems;

/// <summary>
/// 能量机关机制
/// </summary>
public class PowerRuneSystem : ISystem
{
    private static readonly int PowerRuneActivateCacheKey = "PowerRuneActivate".Sum();
    private static readonly int PowerRuneRecordCacheKey = "PowerRuneRecord".Sum();
    private static readonly int PowerRuneStartTimeCacheKey = "PowerRuneStartTime".Sum();

    private static readonly Identity RedPowerRuneIdentity = new Identity(Camp.Red, 333);
    private static readonly Identity BluePowerRuneIdentity = new Identity(Camp.Blue, 333);

    private bool _isPowerRuneClockwise;

    public PowerRuneSystem(ExperienceSystem experienceSystem)
    {
        experienceSystem.OnExpChange += OnExpChange;
    }

    [Inject]
    internal ITimeSystem TimeSystem { get; set; }

    [Inject]
    internal ILogger Logger { get; set; }

    [Inject]
    internal ICommandPublisher Publisher { get; set; }

    [Inject]
    internal BuffSystem BuffSystem { get; set; }

    [Inject]
    internal ICacheProvider<bool> BoolCacheBox { get; set; }

    [Inject]
    internal ICacheProvider<int> IntCacheBox { get; set; }

    [Inject]
    internal ICacheProvider<double> DoubleCacheBox { get; set; }

    [Inject]
    internal EntitySystem EntitySystem { get; set; }

    [Inject]
    internal ExperienceSystem ExperienceSystem { get; set; }

    [Inject]
    internal IMatchConfiguration MatchConfiguration { get; set; }

    public Task Reset(CancellationToken cancellation = new CancellationToken())
    {
        _isPowerRuneClockwise = MatchConfiguration.Random.Next(0, 2) == 0;

        SetPowerRuneCanActivate(Camp.Red, false);
        SetPowerRuneCanActivate(Camp.Blue, false);

        TimeSystem.RegisterOnceAction(
            JudgeSystemStage.Match,
            0,
            async () =>
            {
                await Publisher.PublishAsync(new PowerRuneStopEvent(Camp.Red));
                await Publisher.PublishAsync(new PowerRuneStopEvent(Camp.Blue));
            }
        );

        TimeSystem.RegisterOnceAction(JudgeSystemStage.Match, 5, StartSmallPowerRune);
        TimeSystem.RegisterOnceAction(JudgeSystemStage.Match, 90, StartSmallPowerRune);

        return Task.CompletedTask;
    }

    public double GetPowerRuneTime(Camp camp)
    {
        var startTime = DoubleCacheBox
            .WithReaderNamespace(new Identity(camp, 0))
            .TryLoad(PowerRuneStartTimeCacheKey, out var time)
            ? time
            : 0;

        return TimeSystem.StageTimeElapsed - startTime;
    }

    private void SetPowerRuneStartTime(Camp camp, double time)
    {
        DoubleCacheBox
            .WithWriterNamespace(new Identity(camp, 0))
            .Save(PowerRuneStartTimeCacheKey, time);
    }

    /// <summary>
    /// 小能量机关： 比赛开始 1 分钟后和比赛开始 2 分 30 秒后，能量机关开始旋转，进入可激活状态，能
    /// 量机关进入可激活状态 30 秒后，若其仍未被激活，则将恢复为不可激活状态。若一方小能量机关进
    ///     入已激活状态，另一方小能量机关立即变为不可激活状态。
    /// </summary>
    /// <returns></returns>
    private Task StartSmallPowerRune()
    {
        Logger.Info("Small Power Rune Start");
        SetPowerRuneCanActivate(Camp.Red, true);
        SetPowerRuneCanActivate(Camp.Blue, true);
        SetPowerRuneStartTime(Camp.Red, TimeSystem.StageTimeElapsed);
        TimeSystem.RegisterOnceAction(30, CancelPowerRune);
        return Publisher
            .PublishAsync(new PowerRuneStartEvent(false, default, _isPowerRuneClockwise))
            .AsTask();
    }

    /// <summary>
    /// 大能量机关： 比赛开始 4 分钟、 5 分 15 秒、 6 分 30 秒后，能量机关开始旋转，进入可激活状态，能
    /// 量机关进入可激活状态 30 秒后，若其仍未被激活，则将恢复为不可激活状态。大能量机关的每块装
    ///     甲模块被划分为 1~10 环。
    /// </summary>
    /// <returns></returns>
    private Task StartBigPowerRune()
    {
        var options = BigPowerRuneOptions.RandomPowerRuneOptions();
        Logger.Info($"Big Power Rune Start: A={options.A}, W={options.W}, B={options.B}");
        SetPowerRuneCanActivate(Camp.Red, true);
        SetPowerRuneCanActivate(Camp.Blue, true);
        SetPowerRuneStartTime(Camp.Red, TimeSystem.StageTimeElapsed);
        TimeSystem.RegisterOnceAction(20, CancelPowerRune);
        return Publisher
            .PublishAsync(new PowerRuneStartEvent(true, options, _isPowerRuneClockwise))
            .AsTask();
    }

    private async Task CancelPowerRune()
    {
        var redCanActivate = CanPowerRuneActivate(Camp.Red);
        if (redCanActivate)
        {
            Logger.Info("Cancel Power Rune: Red");
            SetPowerRuneCanActivate(Camp.Red, false);
            await Publisher.PublishAsync(new PowerRuneStopEvent(Camp.Red));
        }

        var blueCanActivate = CanPowerRuneActivate(Camp.Blue);
        if (blueCanActivate)
        {
            Logger.Info("Cancel Power Rune: Blue");
            SetPowerRuneCanActivate(Camp.Blue, false);
            await Publisher.PublishAsync(new PowerRuneStopEvent(Camp.Blue));
        }
    }

    public void OnActivatedSmallPowerRune(Camp camp)
    {
        if (!CanPowerRuneActivate(camp))
        {
            return;
        }

        Logger.Info($"Small Power Rune Activated by {camp}");

        // SetPowerRuneCanActivate(Camp.Red, false);
        // SetPowerRuneCanActivate(Camp.Blue, false);
        //
        // Publisher.PublishAsync(new PowerRuneActivatedEvent(false, default, camp));
        // Publisher.PublishAsync(new PowerRuneStopEvent(camp.GetOppositeCamp()));
            
        // 2025uc v2.1.0
        SetPowerRuneCanActivate(camp, false);
        Publisher.PublishAsync(new PowerRuneActivatedEvent(false, default, camp));
            
        TimeSystem.RegisterOnceAction(
            45,
            () =>
            {
                Publisher.PublishAsync(new PowerRuneStopEvent(camp));
            }
        );

        // 一方机器人成功激活小能量机关后，该方所有机器人获得 25%的防御增益，持续 45 秒
        var robots = EntitySystem
            .GetOperatedEntities<IExperienced>(camp)
            .Where(r => r is IRobot)
            .Select(s => s.Id);

        foreach (var robot in robots)
        {
            BuffSystem.AddBuff(robot, Buffs.DefenceBuff, 0.25f, TimeSpan.FromSeconds(45));
            BuffSystem.AddBuff(
                robot,
                RM2025ucBuffs.SmallPowerRuneBuff,
                0,
                TimeSpan.FromSeconds(45)
            );
        }

        BuffSystem.AddBuff(
            new Identity(camp, Identity.OutpostId),
            Buffs.DefenceBuff,
            0.75f,
            TimeSpan.FromSeconds(45)
        );
        BuffSystem.AddBuff(
            new Identity(camp, Identity.BaseId),
            Buffs.DefenceBuff,
            0.75f,
            TimeSpan.FromSeconds(45)
        );

        SetLastPowerRuneActivateTime(camp, TimeSystem.Time);
    }

    public void OnActivatedBigPowerRune(Camp camp, in PowerRuneActivateRecord record)
    {
        if (!CanPowerRuneActivate(camp))
        {
            return;
        }

        Logger.Info($"Big Power Rune Activated by {camp}, Record: {record.ToString()}");

        SetPowerRuneCanActivate(camp, false);
        Publisher.PublishAsync(new PowerRuneActivatedEvent(true, record, camp));
        TimeSystem.RegisterOnceAction(
            45,
            () =>
            {
                Publisher.PublishAsync(new PowerRuneStopEvent(camp));
            }
        );

        // 一方机器人激活大能量机关后， 系统将根据其击中的总环数为该方所有机器人提供相应的攻击和防御增益，
        // 为该方前哨站、基地提供相应的防御增益，详见“表 5-21 总环数与对应增益”。
        var robots = EntitySystem
            .GetOperatedEntities<IExperienced>(camp)
            .Where(r => r is IRobot)
            .Select(s => s.Id)
            .Append(new Identity(camp, Identity.RedOutpost.Id))
            .Append(new Identity(camp, Identity.RedBase.Id));
        var totalRing = record.Total;
        var attackBuff = GetBigPowerRuneAttackBuffValue(totalRing);
        var defenceBuff = GetBigPowerRuneDefenceBuffValue(totalRing);

        foreach (var robot in robots)
        {
            BuffSystem.AddBuff(robot, Buffs.AttackBuff, attackBuff, TimeSpan.FromSeconds(45));
            BuffSystem.AddBuff(robot, Buffs.DefenceBuff, defenceBuff, TimeSpan.FromSeconds(45));
            BuffSystem.AddBuff(
                robot,
                RM2025ucBuffs.BigPowerRuneBuff,
                0,
                TimeSpan.FromSeconds(45)
            );
        }

        // 同时，大能量机关被激活时，
        // 将有 500 点经验平均分给激活方所有存活的英雄、步兵
        var expRobots = EntitySystem
            .GetOperatedEntities<Hero>(camp)
            .Where(r => !r.IsDead())
            .Cast<IExperienced>()
            .Concat(EntitySystem.GetOperatedEntities<Infantry>(camp).Where(r => !r.IsDead()))
            .ToList();

        if (expRobots.Count > 0)
        {
            const int totalExp = 500;
            var expPerRobot = totalExp / expRobots.Count;

            foreach (var robot in expRobots)
            {
                ExperienceSystem.AddExp(robot, expPerRobot);
            }
        }

        SetLastPowerRuneActivateTime(camp, TimeSystem.Time);
    }

    /// <summary>
    /// 小能量机关增益持续期间内，所有英
    ///     雄、步兵机器人在获得经验时，额外获得原经验 100%的经验， 一方在一次小能量机关增益期间内通
    ///    过此方式最多共获得 800 点额外经验。
    /// </summary>
    /// <param name="experienced"></param>
    /// <param name="exp"></param>
    private void OnExpChange(IExperienced experienced, int exp)
    {
        if (
            !BuffSystem.TryGetBuff(experienced.Id, RM2025ucBuffs.SmallPowerRuneBuff, out Buff _)
        )
            return;
        var expGained = GetSmallPowerRuneSessionExpGained(experienced.Id.Camp);
        if (expGained + exp > 800)
        {
            exp = 800 - expGained;
        }

        if (exp <= 0)
        {
            return;
        }

        SetSmallPowerRuneSessionExpGained(experienced.Id.Camp, expGained + exp);
        ExperienceSystem.AddExp(experienced, exp, true);
    }

    public bool CanPowerRuneActivate(Camp camp)
    {
        return BoolCacheBox
            .WithReaderNamespace(new Identity(camp, 0))
            .TryLoad(PowerRuneActivateCacheKey, out var canActivate) && canActivate;
    }

    private void SetPowerRuneCanActivate(Camp camp, bool canActivate)
    {
        BoolCacheBox
            .WithWriterNamespace(new Identity(camp, 0))
            .Save(PowerRuneActivateCacheKey, canActivate);
    }

    private int GetSmallPowerRuneSessionExpGained(Camp camp)
    {
        return IntCacheBox
            .WithReaderNamespace(new Identity(camp, 0))
            .TryLoad(PowerRuneRecordCacheKey, out var exp)
            ? exp
            : 0;
    }

    private void SetSmallPowerRuneSessionExpGained(Camp camp, int exp)
    {
        IntCacheBox
            .WithWriterNamespace(new Identity(camp, 0))
            .Save(PowerRuneRecordCacheKey, exp);
    }

    /*
     * 表 5-21 总环数与对应增益
总环数区间 攻击增益 防御增益
[5, 15] 150% 25%
(15, 25] 155% 25%
(25, 35] 160% 25%
(35, 40] 200% 25%
(40, 45] 300% 25%
46 340% 30%
47 380% 35%
48 420% 40%
49 460% 45%
50 500% 50%
     */
    public static float GetBigPowerRuneAttackBuffValue(int level)
    {
        return level switch
        {
            >= 5 and <= 15 => 1.5f,
            > 15 and <= 25 => 1.55f,
            > 25 and <= 35 => 1.6f,
            > 35 and <= 40 => 2f,
            > 40 and <= 45 => 3f,
            46 => 3.4f,
            47 => 3.8f,
            48 => 4.2f,
            49 => 4.6f,
            50 => 5f,
            _ => 0,
        };
    }

    public static float GetBigPowerRuneDefenceBuffValue(int level)
    {
        return level switch
        {
            >= 5 and <= 45 => 0.25f,
            46 => 0.3f,
            47 => 0.35f,
            48 => 0.40f,
            49 => 0.45f,
            50 => 0.50f,
            _ => 0,
        };
    }

    public int GetBigPowerRuneChanceMax()
    {
        return TimeSystem.StageTimeElapsed switch
        {
            > 330 => 3,
            > 255 => 2,
            > 180 => 1,
            _ => 0,
        };
    }

    private static readonly int BigPowerRuneChanceElapsedCacheKey =
        "BigPowerRuneChanceElapsed".Sum();

    private int GetBigPowerRuneChanceElapsed(Camp camp)
    {
        return IntCacheBox
            .WithReaderNamespace(new Identity(camp, 333))
            .Load(BigPowerRuneChanceElapsedCacheKey);
    }

    private void SetBigPowerRuneChanceElapsed(Camp camp, int elapsed)
    {
        IntCacheBox
            .WithWriterNamespace(new Identity(camp, 333))
            .Save(BigPowerRuneChanceElapsedCacheKey, elapsed);
    }

    public int GetBigPowerRuneChanceRemaining(Camp camp)
    {
        return GetBigPowerRuneChanceMax() - GetBigPowerRuneChanceElapsed(camp);
    }

    public double GetLastPowerRuneActivateTime(Camp camp)
    {
        return DoubleCacheBox
            .WithReaderNamespace(new Identity(camp, 333))
            .TryLoad(PowerRuneActivateCacheKey, out var time)
            ? time
            : 0;
    }

    private void SetLastPowerRuneActivateTime(Camp camp, double time)
    {
        DoubleCacheBox
            .WithWriterNamespace(new Identity(camp, 333))
            .Save(PowerRuneActivateCacheKey, time);
    }

    public bool CanStartBigPowerRune(Camp camp)
    {
        if (TimeSystem.Stage is not JudgeSystemStage.Match)
            return false;
        if (GetBigPowerRuneChanceMax() <= 0)
            return false;
        if (GetBigPowerRuneChanceRemaining(camp) <= 0)
            return false;
        if (TimeSystem.Time - GetLastPowerRuneActivateTime(camp) <= 20)
            return false;
        return true;
    }

    public void RequestStartBigPowerRune(Camp camp)
    {
        if (!CanStartBigPowerRune(camp))
        {
            return;
        }

        var elapsed = GetBigPowerRuneChanceElapsed(camp);
        SetBigPowerRuneChanceElapsed(camp, elapsed + 1);
        StartBigPowerRune();
    }
}

public readonly struct BigPowerRuneOptions : IEquatable<BigPowerRuneOptions>
{
    public readonly float A;
    public readonly float W;
    public readonly float B;

    public float GetSpeed(double time)
    {
        return (float)(A * Math.Sin(W * time) + B);
    }

    public BigPowerRuneOptions(float a, float w, float b)
    {
        A = a;
        W = w;
        B = b;
    }

    public static BigPowerRuneOptions RandomPowerRuneOptions()
    {
        var rand = new Random();

        var a = (float)rand.NextDouble() * (1.045f - 0.780f) + 0.780f;
        var w = (float)rand.NextDouble() * (2f - 1.884f) + 1.884f;
        var b = 2.090f - a;

        return new BigPowerRuneOptions(a, w, b);
    }

    public bool Equals(BigPowerRuneOptions other)
    {
        return A.Equals(other.A) && W.Equals(other.W) && B.Equals(other.B);
    }

    public override bool Equals(object obj)
    {
        return obj is BigPowerRuneOptions other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(A, W, B);
    }

    public static bool operator ==(BigPowerRuneOptions left, BigPowerRuneOptions right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(BigPowerRuneOptions left, BigPowerRuneOptions right)
    {
        return !left.Equals(right);
    }
}

public readonly struct PowerRuneActivateRecord : IEquatable<PowerRuneActivateRecord>
{
    public readonly int Ring1;
    public readonly int Ring2;
    public readonly int Ring3;
    public readonly int Ring4;
    public readonly int Ring5;

    public PowerRuneActivateRecord(int ring1, int ring2, int ring3, int ring4, int ring5)
    {
        Ring1 = ring1;
        Ring2 = ring2;
        Ring3 = ring3;
        Ring4 = ring4;
        Ring5 = ring5;
    }

    public static implicit operator PowerRuneActivateRecord(Queue<int> rings)
    {
        return new PowerRuneActivateRecord(
            rings.Dequeue(),
            rings.Dequeue(),
            rings.Dequeue(),
            rings.Dequeue(),
            rings.Dequeue()
        );
    }

    public override string ToString()
    {
        return $"Ring1: {Ring1}, Ring2: {Ring2}, Ring3: {Ring3}, Ring4: {Ring4}, Ring5: {Ring5}, Total: {Total}";
    }

    public int Total => Ring1 + Ring2 + Ring3 + Ring4 + Ring5;

    public bool Equals(PowerRuneActivateRecord other)
    {
        return Ring1 == other.Ring1
               && Ring2 == other.Ring2
               && Ring3 == other.Ring3
               && Ring4 == other.Ring4
               && Ring5 == other.Ring5;
    }

    public override bool Equals(object obj)
    {
        return obj is PowerRuneActivateRecord other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Ring1, Ring2, Ring3, Ring4, Ring5);
    }

    public static bool operator ==(PowerRuneActivateRecord left, PowerRuneActivateRecord right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(PowerRuneActivateRecord left, PowerRuneActivateRecord right)
    {
        return !left.Equals(right);
    }
}