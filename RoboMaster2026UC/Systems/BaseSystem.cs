using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RoboSouls.JudgeSystem.Entities;
using RoboSouls.JudgeSystem.Events;
using RoboSouls.JudgeSystem.RoboMaster2026UC.Entities;
using RoboSouls.JudgeSystem.RoboMaster2026UC.Events;
using RoboSouls.JudgeSystem.Systems;
using VContainer;
using VitalRouter;

namespace RoboSouls.JudgeSystem.RoboMaster2026UC.Systems;

/// <summary>
/// 基地机制
/// </summary>
[Routes]
public sealed partial class BaseSystem : ISystem
{
    [Inject]
    internal void Inject(Router router)
    {
        MapTo(router);
    }

    /// <summary>
    /// 红方基地增益点
    /// </summary>
    public static readonly Identity RedBaseZoneId = new Identity(Camp.Red, 50);

    /// <summary>
    /// 蓝方基地增益点
    /// </summary>
    public static readonly Identity BlueBaseZoneId = new Identity(Camp.Blue, 50);

    private static readonly int BaseZoneDeactivatedCacheKey = "BaseZoneDeactivated".Sum();

    [Inject]
    internal ICacheProvider<bool> BoolCacheBox { get; set; }

    [Inject]
    internal LifeSystem LifeSystem { get; set; }

    [Inject]
    internal EntitySystem EntitySystem { get; set; }

    [Inject]
    internal ICommandPublisher Publisher { get; set; }

    [Inject]
    internal BuffSystem BuffSystem { get; set; }

    [Inject]
    internal ITimeSystem TimeSystem { get; set; }

    [Inject]
    internal JudgeBotSystem JudgeBotSystem { get; set; }

    [Inject]
    internal ZoneSystem ZoneSystem { get; set; }

    public Task Reset(CancellationToken cancellation = new CancellationToken())
    {
        var redBase = EntitySystem.Entities[Identity.RedBase] as Base;
        var blueBase = EntitySystem.Entities[Identity.BlueBase] as Base;

        LifeSystem.SetInvincible(redBase, true);
        LifeSystem.SetInvincible(blueBase, true);

        SetArmorOpen(redBase, false);
        SetArmorOpen(blueBase, false);

        SetBaseZoneDeactivated(Camp.Red, false);
        SetBaseZoneDeactivated(Camp.Blue, false);

        TimeSystem.RegisterOnceAction(
            JudgeSystemStage.Countdown,
            1,
            FalseStartDetectLoop(Camp.Red)
        );
        TimeSystem.RegisterOnceAction(
            JudgeSystemStage.Countdown,
            1,
            FalseStartDetectLoop(Camp.Blue)
        );
        TimeSystem.RegisterOnceAction(
            JudgeSystemStage.Countdown,
            2,
            FalseStartDetectLoop(Camp.Red)
        );
        TimeSystem.RegisterOnceAction(
            JudgeSystemStage.Countdown,
            2,
            FalseStartDetectLoop(Camp.Blue)
        );
        TimeSystem.RegisterOnceAction(
            JudgeSystemStage.Countdown,
            3,
            FalseStartDetectLoop(Camp.Red)
        );
        TimeSystem.RegisterOnceAction(
            JudgeSystemStage.Countdown,
            3,
            FalseStartDetectLoop(Camp.Blue)
        );
        TimeSystem.RegisterOnceAction(
            JudgeSystemStage.Countdown,
            4,
            FalseStartDetectLoop(Camp.Red)
        );
        TimeSystem.RegisterOnceAction(
            JudgeSystemStage.Countdown,
            4,
            FalseStartDetectLoop(Camp.Blue)
        );

        return Task.CompletedTask;
    }

    /// <summary>
    /// 抢跑检测
    /// </summary>
    /// <param name="camp"></param>
    /// <returns></returns>
    private Action FalseStartDetectLoop(Camp camp)
    {
        var baseZone = camp == Camp.Red ? RedBaseZoneId : BlueBaseZoneId;

        return () =>
        {
            foreach (
                var robot in EntitySystem
                    .Entities.Values.OfType<IHealthed>()
                    .OfType<IRobot>()
                    .Select(r => r.Id)
                    .Where(i => i.Camp == camp)
                    .Where(i => !ZoneSystem.IsInZone(i, baseZone))
            )
            {
                JudgeBotSystem.Penalty(
                    Identity.Server,
                    robot,
                    PenaltyType.RedCard,
                    JudgeBotSystem.PenaltyReasonFalseStart
                );
            }
        };
    }

    /// <summary>
    /// 比赛开始时，基地处于无敌状态。当一方前哨站被击毁，该方基地的无敌状态解除
    /// </summary>
    /// <param name="evt"></param>
    [Route]
    private void OnKill(KillEvent evt)
    {
        if (!evt.Victim.IsOutpost())
            return;

        Base b;
        if (evt.Victim.Camp == Camp.Red)
        {
            b = EntitySystem.Entities[Identity.RedBase] as Base;
        }
        else
        {
            b = EntitySystem.Entities[Identity.BlueBase] as Base;
        }

        LifeSystem.SetInvincible(b, false);
    }

    /// <summary>
    /// 基地增益点机制
    /// 基地增益点只可由己方英雄、步兵、哨兵机器人占领。同一方的多台机器人可同时占领基地增益点。
    /// 在七分钟比赛阶段， 占领己方基地增益点的机器人可获得 50%防御增益。
    /// 占领己方基地增益点的机器人在比赛开始 2-3 分钟、 3-5 分钟、 5-7 分钟时分别可获得 2、 3、 5 倍射击热量
    ///    冷却增益
    /// </summary>
    /// <param name="evt"></param>
    [Route]
    private void OnEnterBaseZone(EnterZoneEvent evt)
    {
        if (TimeSystem.Stage != JudgeSystemStage.Match)
            return;
        if (evt.ZoneId != RedBaseZoneId && evt.ZoneId != BlueBaseZoneId)
            return;
        if (evt.OperatorId.Camp != evt.ZoneId.Camp)
            return;
        if (IsBaseZoneDeactivated(evt.ZoneId.Camp))
            return;

        BuffSystem.AddBuff(evt.OperatorId, Buffs.DefenceBuff, 0.5f, TimeSpan.MaxValue);
        var cooldownBuffValue = TimeSystem.StageTimeElapsed switch
        {
            >= 120 and < 180 => 2,
            >= 180 and < 300 => 3,
            >= 300 => 5,
            _ => 0,
        };
        if (cooldownBuffValue > 0)
        {
            BuffSystem.AddBuff(
                evt.OperatorId,
                Buffs.CoolDownBuff,
                cooldownBuffValue,
                TimeSpan.MaxValue
            );
        }
            
        BuffSystem.RemoveBuff(evt.OperatorId, RM2026ucBuffs.WeakenedBuff);
    }

    [Route]
    private void OnExitBaseZone(ExitZoneEvent evt)
    {
        if (evt.ZoneId != RedBaseZoneId && evt.ZoneId != BlueBaseZoneId)
            return;
        if (evt.OperatorId.Camp != evt.ZoneId.Camp)
            return;

        if (TimeSystem.Stage == JudgeSystemStage.Match)
        {
            BuffSystem.RemoveBuff(evt.OperatorId, Buffs.DefenceBuff);
            BuffSystem.RemoveBuff(evt.OperatorId, Buffs.CoolDownBuff);
        }
    }

    private void CheckBaseHealth(Camp camp)
    {
        var baseId = camp switch
        {
            Camp.Red => Identity.RedBase,
            Camp.Blue => Identity.BlueBase,
            _ => throw new ArgumentOutOfRangeException(),
        };

        var b = EntitySystem.Entities[baseId] as Base;

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

        BoolCacheBox.WithWriterNamespace(b.Id).Save(Base.ArmorOpenCacheKey, open);

        Publisher.PublishAsync(new BaseArmorOpenEvent(b.Id));
    }

    public bool IsBaseZoneDeactivated(Camp camp)
    {
        var id = camp == Camp.Red ? RedBaseZoneId : BlueBaseZoneId;
        return BoolCacheBox
            .WithReaderNamespace(id)
            .TryLoad(BaseZoneDeactivatedCacheKey, out var deactivated) && deactivated;
    }

    private void SetBaseZoneDeactivated(Camp camp, bool deactivated)
    {
        var id = camp == Camp.Red ? RedBaseZoneId : BlueBaseZoneId;
        BoolCacheBox.WithWriterNamespace(id).Save(BaseZoneDeactivatedCacheKey, deactivated);
    }
}