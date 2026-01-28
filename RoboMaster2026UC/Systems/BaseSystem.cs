using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RoboSouls.JudgeSystem.Attributes;
using RoboSouls.JudgeSystem.Entities;
using RoboSouls.JudgeSystem.Events;
using RoboSouls.JudgeSystem.RoboMaster2026UC.Entities;
using RoboSouls.JudgeSystem.RoboMaster2026UC.Events;
using RoboSouls.JudgeSystem.Systems;
using VContainer;
using VitalRouter;

namespace RoboSouls.JudgeSystem.RoboMaster2026UC.Systems;

/// <summary>
///     基地机制
/// </summary>
[Routes]
public sealed partial class BaseSystem(
    ICacheProvider<bool> boolCacheBox,
    LifeSystem lifeSystem,
    EntitySystem entitySystem,
    ICommandPublisher publisher,
    BuffSystem buffSystem,
    ITimeSystem timeSystem,
    JudgeBotSystem judgeBotSystem,
    ICacheProvider<int> intCacheBox,
    OutpostSystem outpostSystem,
    ILogger logger,
    ZoneSystem zoneSystem)
    : ISystem
{
    /// <summary>
    ///     红方基地增益点
    /// </summary>
    public static readonly Identity RedBaseZoneId = new(Camp.Red, 50);

    /// <summary>
    ///     蓝方基地增益点
    /// </summary>
    public static readonly Identity BlueBaseZoneId = new(Camp.Blue, 50);

    [Property(nameof(boolCacheBox), PropertyStorageMode.Camp)]
    public partial bool BaseZoneDeactivated { get; private set; }

    /// <summary>
    ///     基地承受的总伤害值
    /// </summary>
    /// <returns></returns>
    [Property(nameof(intCacheBox), PropertyStorageMode.Camp)]
    public partial int BaseTotalDamage { get; private set; }

    /// <summary>
    ///     已使用重建前哨站次数
    /// </summary>
    [Property(nameof(intCacheBox), PropertyStorageMode.Camp)]
    public partial int OutpostRebuildCountUsed { get; private set; }

    public Task Reset(CancellationToken cancellation = new())
    {
        var redBase = entitySystem.Entities[Identity.RedBase] as Base;
        var blueBase = entitySystem.Entities[Identity.BlueBase] as Base;

        lifeSystem.SetInvincible(redBase!, true);
        lifeSystem.SetInvincible(blueBase!, true);

        SetArmorOpen(redBase!, false);
        SetArmorOpen(blueBase!, false);

        SetBaseZoneDeactivated(Camp.Red, false);
        SetBaseZoneDeactivated(Camp.Blue, false);

        for (var i = 1; i <= 4; i++)
        {
            timeSystem.RegisterOnceAction(
                JudgeSystemStage.Countdown,
                i,
                FalseStartDetectLoop(Camp.Red)
            );
            timeSystem.RegisterOnceAction(
                JudgeSystemStage.Countdown,
                i,
                FalseStartDetectLoop(Camp.Blue)
            );
        }

        timeSystem.RegisterRepeatAction(1,
            () => Task.WhenAll(RebuildOutpostDetect(Camp.Blue), RebuildOutpostDetect(Camp.Red)));

        return Task.CompletedTask;
    }

    [Inject]
    internal void Inject(Router router)
    {
        MapTo(router);
    }

    /// <summary>
    ///     抢跑检测
    /// </summary>
    /// <param name="camp"></param>
    /// <returns></returns>
    private Action FalseStartDetectLoop(Camp camp)
    {
        var baseZone = camp == Camp.Red ? RedBaseZoneId : BlueBaseZoneId;

        return () =>
        {
            foreach (
                var robot in entitySystem
                    .Entities.Values.OfType<IHealthed>()
                    .OfType<IRobot>()
                    .Select(r => r.Id)
                    .Where(i => i.Camp == camp)
                    .Where(i => !zoneSystem.IsInZone(i, baseZone))
            )
                judgeBotSystem.Penalty(
                    Identity.Server,
                    robot,
                    PenaltyType.RedCard,
                    JudgeBotSystem.PenaltyReasonFalseStart
                );
        };
    }

    /// <summary>
    ///     比赛开始时，基地处于无敌状态。当一方前哨站被击毁，该方基地的无敌状态解除
    /// </summary>
    /// <param name="evt"></param>
    [Route]
    private void OnKill(KillEvent evt)
    {
        if (!evt.Victim.IsOutpost())
            return;

        Base b;
        if (evt.Victim.Camp == Camp.Red)
            b = entitySystem.Entities[Identity.RedBase] as Base ?? throw new InvalidOperationException();
        else
            b = entitySystem.Entities[Identity.BlueBase] as Base ?? throw new InvalidOperationException();

        lifeSystem.SetInvincible(b, false);
    }

    /// <summary>
    ///     基地增益点机制
    ///     基地增益点只可由己方英雄、步兵、哨兵机器人占领。同一方的多台机器人可同时占领基地增益点。
    ///     在七分钟比赛阶段， 占领己方基地增益点的机器人可获得 50%防御增益。
    ///     占领己方基地增益点的机器人在比赛开始 2-3 分钟、 3-5 分钟、 5-7 分钟时分别可获得 2、 3、 5 倍射击热量
    ///     冷却增益
    /// </summary>
    /// <param name="evt"></param>
    [Route]
    private void OnEnterBaseZone(EnterZoneEvent evt)
    {
        if (timeSystem.Stage != JudgeSystemStage.Match)
            return;
        if (evt.ZoneId != RedBaseZoneId && evt.ZoneId != BlueBaseZoneId)
            return;
        if (evt.OperatorId.Camp != evt.ZoneId.Camp)
            return;
        if (GetBaseZoneDeactivated(evt.ZoneId.Camp))
            return;

        buffSystem.AddBuff(evt.OperatorId, Buffs.DefenceBuff, 0.5f, TimeSpan.MaxValue);
        var cooldownBuffValue = timeSystem.StageTimeElapsed switch
        {
            >= 120 and < 180 => 2,
            >= 180 and < 300 => 3,
            >= 300 => 5,
            _ => 0
        };
        if (cooldownBuffValue > 0)
            buffSystem.AddBuff(
                evt.OperatorId,
                Buffs.CoolDownBuff,
                cooldownBuffValue,
                TimeSpan.MaxValue
            );

        buffSystem.RemoveBuff(evt.OperatorId, RM2026ucBuffs.WeakenedBuff);
    }

    [Route]
    private void OnExitBaseZone(ExitZoneEvent evt)
    {
        if (evt.ZoneId != RedBaseZoneId && evt.ZoneId != BlueBaseZoneId)
            return;
        if (evt.OperatorId.Camp != evt.ZoneId.Camp)
            return;

        if (timeSystem.Stage == JudgeSystemStage.Match)
        {
            buffSystem.RemoveBuff(evt.OperatorId, Buffs.DefenceBuff);
            buffSystem.RemoveBuff(evt.OperatorId, Buffs.CoolDownBuff);
        }
    }

    private void CheckBaseHealth(Camp camp)
    {
        var baseId = camp switch
        {
            Camp.Red => Identity.RedBase,
            Camp.Blue => Identity.BlueBase,
            _ => throw new ArgumentOutOfRangeException()
        };

        var b = entitySystem.Entities[baseId] as Base;

        if (b.Health > 2000)
            return;

        SetArmorOpen(b, true);
    }

    [Route]
    private void OnDamage(DamageCommand evt)
    {
        if (evt.Victim is not Base)
            return;

        CheckBaseHealth(evt.Victim.Id.Camp);
    }

    [Route]
    private void OnDartHit(DartHitEvent evt)
    {
        CheckBaseHealth(evt.Camp.GetOppositeCamp());
    }

    internal void SetArmorOpen(Base b, bool open)
    {
        if (b.IsArmorOpen == open)
            return;

        boolCacheBox.WithWriterNamespace(b.Id).Save(Base.ArmorOpenCacheKey, open);

        publisher.PublishAsync(new BaseArmorOpenEvent(b.Id));
    }

    public void AddHealth(Camp camp, uint amount)
    {
        var b = entitySystem.Entities[camp == Camp.Red ? Identity.RedBase : Identity.BlueBase] as Base;
        lifeSystem.IncreaseHealth(b, amount);
    }


    /// <summary>
    ///     已获得重建前哨站次数
    /// </summary>
    /// <param name="camp"></param>
    /// <returns></returns>
    public int GetRebuildOutpostCountTotal(Camp camp)
    {
        if (timeSystem.Stage != JudgeSystemStage.Match) return 0;
        if (timeSystem.StageTimeElapsed >= 300) return 0;

        return GetBaseTotalDamage(camp) / 1000;
    }

    public int GetOutpostBuildCountRemaining(Camp camp)
    {
        var total = GetRebuildOutpostCountTotal(camp);
        if (total <= 0) return 0;


        return Math.Max(0, total - GetOutpostRebuildCountUsed(camp));
    }

    /// <summary>
    ///     一方每累计损失 1000 点基地血量，获得 1 次可积累的“重建前哨站“机会。 英雄、步兵、哨兵机器人通
    ///     过连续检测己方前哨站增益点场地交互模块卡 10 秒，或工程机器人连续检测己方前哨站增益点场地交互
    ///     模块卡 5 秒（不同机器人占领时长独立计算），即可“重建” 被击毁的前哨站，使前哨站恢复存活状态并
    ///     具有 750 点血量。在比赛开始 5 分钟后，前哨站不能够再被重建。
    /// </summary>
    /// <returns></returns>
    public bool TryRebuildOutpost(Camp camp)
    {
        if (!outpostSystem.IsOutpostDestroyed(camp, out var outpost)) return false;

        if (GetOutpostBuildCountRemaining(camp) <= 0) return false;

        lifeSystem.IncreaseHealth(outpost, 750);

        SetOutpostRebuildCountUsed(camp, GetOutpostRebuildCountUsed(camp) + 1);
        logger.Info($"{camp} rebuilt outpost");
        return true;
    }

    private Task RebuildOutpostDetect(Camp camp)
    {
        if (timeSystem.Stage != JudgeSystemStage.Match) return Task.CompletedTask;

        var robots = entitySystem
            .Entities.Values.OfType<IHealthed>()
            .Where(i => !i.IsDead())
            .OfType<IRobot>()
            .Select(r => r.Id)
            .Where(i => i.Camp == camp)
            .Where(i => entitySystem.HasOperator(i));

        return Task.WhenAll(robots.Select(RebuildOutpostDetectLoopFor));
    }

    private Task RebuildOutpostDetectLoopFor(Identity id)
    {
        if (!outpostSystem.IsOutpostDestroyed(id.Camp, out _)) return Task.CompletedTask;

        if (!buffSystem.TryGetBuff(id, RM2026ucBuffs.RebuildingOutpost, out float startTime)) return Task.CompletedTask;

        var duration = timeSystem.TimeAsFloat - startTime;
        float threshold;
        if (id.IsHero() || id.IsInfantry() || id.IsSentry())
            threshold = 10;
        else if (id.IsEngineer())
            threshold = 5;
        else
            return Task.CompletedTask;

        if (duration < threshold) return Task.CompletedTask;

        TryRebuildOutpost(id.Camp);
        return Task.CompletedTask;
    }
}