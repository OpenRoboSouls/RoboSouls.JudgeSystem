using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RoboSouls.JudgeSystem.Entities;
using RoboSouls.JudgeSystem.Events;
using RoboSouls.JudgeSystem.RoboMaster2025UC.Entities;
using RoboSouls.JudgeSystem.RoboMaster2025UC.Events;
using RoboSouls.JudgeSystem.Systems;
using VContainer;
using VitalRouter;

namespace RoboSouls.JudgeSystem.RoboMaster2025UC.Systems;

/// <summary>
///     堡垒机制
///     堡垒增益点仅能被己方步兵机器人或哨兵机器人占领。 同一时间仅能被一台机器人占领。
///     在己方前哨站被击毁前，堡垒增益点处于失效状态。 在己方前哨站被击毁后，堡垒增益点生效。
///     定义己方基地血量上限减去现有基地血量的差值为∆ 。
///     占领堡垒增益点的机器人处于无敌状态， 且获得值为 w 的射击热量冷却增益， w 与Δ相关，具体关系如下：
///     w = ∆ / 20（向下取整）， w 的上限为 150。
///     堡垒增益区拥有独立的允许发弹量上限 N，占领堡垒增益区的机器人在发射弹丸时，优先消耗堡垒储备的
///     允许发弹量，储备允许发弹量耗尽后再消耗机器人的允许发弹量。在 N 增加时，提升的允许发弹量上限将
///     立即被添加至堡垒的储备允许发弹量。
///     N = 200 + 4 × ∆ / 15（向下取整）， N 至多为 1000。
///     示例：
///     一台射击热量冷却速率为 80/秒的机器人，同时获得了堡垒增益点提供的值为 150 的热量增益和 5 倍
///     热量冷却增益。由于 80*5>80+150，故机器人实际射击热量冷却速率为 400/秒。
/// </summary>
[Routes]
public sealed partial class FortressSystem : ISystem
{
    public static readonly Identity RedFortressZoneId = new(Camp.Red, 90);
    public static readonly Identity BlueFortressZoneId = new(Camp.Blue, 90);

    private static readonly int FortressActiveCacheKey = "FortressActive".Sum();
    private static readonly int CurrentFortressUserCacheKey = "CurrentFortressUser".Sum();
    private static readonly int AmmoSpentCacheKey = "AmmoSpent".Sum();

    private static readonly int LastEnterOppositeFortressTimeKey =
        "LastEnterOppositeFortressTime".Sum();

    private static readonly int LastLeaveOppositeFortressTimeKey =
        "LastEnterOppositeFortressTime".Sum();

    private readonly Dictionary<Camp, Identity> _lastEnterOppositeFortressUser = new()
    {
        { Camp.Red, Identity.Spectator },
        { Camp.Blue, Identity.Spectator }
    };

    [Inject] internal BuffSystem BuffSystem { get; set; }

    [Inject] internal EntitySystem EntitySystem { get; set; }

    [Inject] internal ITimeSystem TimeSystem { get; set; }

    [Inject] internal ICacheProvider<bool> BoolCacheBox { get; set; }

    [Inject] internal ICacheProvider<ushort> UshortCacheBox { get; set; }

    [Inject] internal ICacheProvider<int> IntCacheBox { get; set; }

    [Inject] internal ICacheProvider<double> DoubleCacheBox { get; set; }

    [Inject] internal ICommandPublisher CommandPublisher { get; set; }

    [Inject] internal BattleSystem BattleSystem { get; set; }

    [Inject] internal BaseSystem BaseSystem { get; set; }

    public Task Reset(CancellationToken cancellation = new())
    {
        SetFortressActive(Camp.Red, false);
        SetFortressActive(Camp.Blue, false);
        SetCurrentFortressUser(Camp.Red, 0);
        SetCurrentFortressUser(Camp.Blue, 0);

        TimeSystem.RegisterRepeatAction(1, FortressOccupierUpdateLoop);
        TimeSystem.RegisterRepeatAction(
            1,
            () =>
            {
                EnterOppositeFortressSettlementFor(Camp.Red);
                EnterOppositeFortressSettlementFor(Camp.Blue);
            }
        );
        return Task.CompletedTask;
    }

    [Inject]
    internal void Inject(Router router)
    {
        MapTo(router);
    }

    public bool IsFortressActive(Camp camp)
    {
        var id = camp switch
        {
            Camp.Red => RedFortressZoneId,
            Camp.Blue => BlueFortressZoneId,
            _ => throw new ArgumentOutOfRangeException()
        };

        return BoolCacheBox
            .WithReaderNamespace(id)
            .TryLoad(FortressActiveCacheKey, out var active) && active;
    }

    private void SetFortressActive(Camp camp, bool active)
    {
        if (IsFortressActive(camp) == active)
            return;

        var id = camp switch
        {
            Camp.Red => RedFortressZoneId,
            Camp.Blue => BlueFortressZoneId,
            _ => throw new ArgumentOutOfRangeException()
        };

        BoolCacheBox.WithWriterNamespace(id).Save(FortressActiveCacheKey, active);

        CommandPublisher.PublishAsync(new FortressActivateEvent(camp));
    }

    public ushort GetCurrentFortressUserID(Camp camp)
    {
        var id = camp switch
        {
            Camp.Red => RedFortressZoneId,
            Camp.Blue => BlueFortressZoneId,
            _ => throw new ArgumentOutOfRangeException()
        };

        return UshortCacheBox
            .WithReaderNamespace(id)
            .TryLoad(CurrentFortressUserCacheKey, out var user)
            ? user
            : default;
    }

    private void SetCurrentFortressUser(Camp camp, ushort user)
    {
        if (GetCurrentFortressUserID(camp) == user)
            return;

        if (user != 0)
            CommandPublisher.PublishAsync(new FortressEnterEvent(new Identity(camp, user)));
        else
            CommandPublisher.PublishAsync(
                new FortressExitEvent(new Identity(camp, GetCurrentFortressUserID(camp)))
            );

        var id = camp switch
        {
            Camp.Red => RedFortressZoneId,
            Camp.Blue => BlueFortressZoneId,
            _ => throw new ArgumentOutOfRangeException()
        };

        UshortCacheBox.WithWriterNamespace(id).Save(CurrentFortressUserCacheKey, user);
    }

    /// <summary>
    ///     获取热量增益
    /// </summary>
    /// <param name="camp"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public int GetCooldownDelta(Camp camp)
    {
        var baseId = camp switch
        {
            Camp.Red => Identity.RedBase,
            Camp.Blue => Identity.BlueBase,
            _ => throw new ArgumentOutOfRangeException()
        };

        var b = EntitySystem.Entities[baseId] as Base;
        var delta = RM2025ucPerformanceSystem.BaseMaxHealth - b.Health;

        var w = Math.Floor(delta / 20f);
        w = Math.Min(w, 150);

        return (int)w;
    }

    /// <summary>
    ///     获取允许发弹量上限
    /// </summary>
    /// <param name="camp"></param>
    /// <returns></returns>
    public int GetAmmoAmount(Camp camp)
    {
        var baseId = camp switch
        {
            Camp.Red => Identity.RedBase,
            Camp.Blue => Identity.BlueBase,
            _ => throw new ArgumentOutOfRangeException()
        };

        var b = EntitySystem.Entities[baseId] as Base;
        var delta = RM2025ucPerformanceSystem.BaseMaxHealth - b.Health;

        var n = 200 + (int)Math.Floor(4 * delta / 15f);
        n = Math.Min(n, 1000);

        return n;
    }

    /// <summary>
    ///     获取已消耗弹药
    /// </summary>
    /// <param name="camp"></param>
    /// <returns></returns>
    public int GetAmmoSpent(Camp camp)
    {
        var id = camp switch
        {
            Camp.Red => RedFortressZoneId,
            Camp.Blue => BlueFortressZoneId,
            _ => throw new ArgumentOutOfRangeException()
        };

        return IntCacheBox.WithReaderNamespace(id).TryLoad(AmmoSpentCacheKey, out var spent)
            ? spent
            : 0;
    }

    /// <summary>
    ///     获取允许发弹量
    /// </summary>
    /// <param name="camp"></param>
    /// <returns></returns>
    public int GetAmmoAllowance(Camp camp)
    {
        return GetAmmoAmount(camp) - GetAmmoSpent(camp);
    }

    private void SetAmmoSpent(Camp camp, int spent)
    {
        var id = camp switch
        {
            Camp.Red => RedFortressZoneId,
            Camp.Blue => BlueFortressZoneId,
            _ => throw new ArgumentOutOfRangeException()
        };

        IntCacheBox.WithWriterNamespace(id).Save(AmmoSpentCacheKey, spent);
    }

    [Route]
    private void OnEnterZone(EnterZoneEvent evt)
    {
        if (evt.ZoneId != RedFortressZoneId && evt.ZoneId != BlueFortressZoneId)
            return;
        if (TimeSystem.Stage is not JudgeSystemStage.Match)
            return;
        if (
            !EntitySystem.TryGetOperatedEntity(evt.OperatorId, out IRobot r)
            || r is not (Infantry or Sentry)
        )
            return;
        if (evt.OperatorId.Camp == evt.ZoneId.Camp)
        {
            if (!IsFortressActive(evt.OperatorId.Camp))
                return;

            if (GetCurrentFortressUserID(evt.OperatorId.Camp) == 0)
                SetCurrentFortressUser(evt.OperatorId.Camp, evt.OperatorId.Id);
        }
        else
        {
            var outpostId =
                evt.ZoneId.Camp == Camp.Red ? Identity.RedOutpost : Identity.BlueOutpost;
            var outpost = EntitySystem.Entities[outpostId] as Outpost;
            if (TimeSystem.StageTimeElapsed < 180 || !outpost.IsDead())
                return;

            if (GetLastEnterOppositeFortressTime(evt.OperatorId.Camp) <= 0)
                SetLastEnterOppositeFortressTime(evt.OperatorId.Camp, TimeSystem.Time);

            if (GetLastEnterOppositeFortressUser(evt.OperatorId.Camp) == Identity.Spectator)
                SetLastEnterOppositeFortressUser(evt.OperatorId);
        }
    }

    [Route]
    private void OnExitZone(ExitZoneEvent evt)
    {
        if (evt.ZoneId != RedFortressZoneId && evt.ZoneId != BlueFortressZoneId)
            return;
        if (TimeSystem.Stage is not JudgeSystemStage.Match)
            return;
        if (
            !EntitySystem.TryGetOperatedEntity(evt.OperatorId, out IRobot r)
            || r is not (Infantry or Sentry)
        )
            return;

        if (evt.OperatorId.Camp == evt.ZoneId.Camp)
        {
            if (evt.OperatorId.Id == GetCurrentFortressUserID(evt.OperatorId.Camp))
            {
                SetCurrentFortressUser(evt.OperatorId.Camp, 0);
                SetLastLeaveOppositeFortressTime(evt.OperatorId.Camp, TimeSystem.Time);
            }
        }
        else
        {
            var outpostId =
                evt.ZoneId.Camp == Camp.Red ? Identity.RedOutpost : Identity.BlueOutpost;
            var outpost = EntitySystem.Entities[outpostId] as Outpost;
            if (TimeSystem.StageTimeElapsed < 180 || !outpost.IsDead())
                return;

            if (GetLastEnterOppositeFortressUser(evt.OperatorId.Camp).Id == evt.OperatorId.Id)
                SetLastEnterOppositeFortressUser(Identity.Spectator);
        }
    }

    [Route]
    private void OnKill(KillEvent evt)
    {
        if (TimeSystem.Stage is not JudgeSystemStage.Match)
            return;
        if (evt.Victim.IsOutpost()) SetFortressActive(evt.Victim.Camp, true);
    }

    private void FortressOccupierUpdateLoop()
    {
        if (TimeSystem.Stage is not JudgeSystemStage.Match)
            return;

        FortressOccupierUpdateLoopFor(Camp.Red);
        FortressOccupierUpdateLoopFor(Camp.Blue);
    }

    private void FortressOccupierUpdateLoopFor(Camp camp)
    {
        var id = new Identity(camp, GetCurrentFortressUserID(camp));
        if (id.Id == 0)
            return;

        BuffSystem.AddBuff(
            id,
            Buffs.CoolDownAmountBuff,
            GetCooldownDelta(camp),
            TimeSpan.FromSeconds(1)
        );
        BuffSystem.AddBuff(id, Buffs.CoolDownBuff, 1, TimeSpan.FromSeconds(1));
    }

    [Route]
    private void OnEnterFortress(FortressEnterEvent evt)
    {
        if (TimeSystem.Stage is not JudgeSystemStage.Match)
            return;

        BuffSystem.AddBuff(evt.Operator, RM2025ucBuffs.FortressBuff, 1, TimeSpan.MaxValue);
        BuffSystem.AddBuff(evt.Operator, Buffs.DefenceBuff, 0.5f, TimeSpan.MaxValue);
    }

    [Route]
    private void OnExitFortress(FortressExitEvent evt)
    {
        if (TimeSystem.Stage is not JudgeSystemStage.Match)
            return;

        BuffSystem.RemoveBuff(evt.Operator, RM2025ucBuffs.FortressBuff);
        BuffSystem.RemoveBuff(evt.Operator, Buffs.DefenceBuff);
        BuffSystem.RemoveBuff(evt.Operator, Buffs.CoolDownAmountBuff);
    }

    [Route]
    private void OnShoot(ShootCommand command)
    {
        if (TimeSystem.Stage is not JudgeSystemStage.Match)
            return;
        if (command.Shooter.Id.Id != GetCurrentFortressUserID(command.Shooter.Id.Camp))
            return;
        var fortressAmmo = GetAmmoAllowance(command.Shooter.Id.Camp);
        if (fortressAmmo <= 0)
            return;

        var payback = Math.Min(command.Amount, fortressAmmo);
        SetAmmoSpent(command.Shooter.Id.Camp, GetAmmoSpent(command.Shooter.Id.Camp) + payback);

        BattleSystem.SetAmmoAllowance(command.Shooter, command.Shooter.AmmoAllowance + payback);
    }

    private double GetLastEnterOppositeFortressTime(Camp camp)
    {
        var id = camp switch
        {
            Camp.Red => BlueFortressZoneId,
            Camp.Blue => RedFortressZoneId,
            _ => throw new ArgumentOutOfRangeException()
        };

        return DoubleCacheBox.WithReaderNamespace(id).Load(LastEnterOppositeFortressTimeKey);
    }

    private void SetLastEnterOppositeFortressTime(Camp camp, double time)
    {
        var id = camp switch
        {
            Camp.Red => BlueFortressZoneId,
            Camp.Blue => RedFortressZoneId,
            _ => throw new ArgumentOutOfRangeException()
        };

        DoubleCacheBox.WithWriterNamespace(id).Save(LastEnterOppositeFortressTimeKey, time);
    }

    private double GetLastLeaveOppositeFortressTime(Camp camp)
    {
        var id = camp switch
        {
            Camp.Red => BlueFortressZoneId,
            Camp.Blue => RedFortressZoneId,
            _ => throw new ArgumentOutOfRangeException()
        };

        return DoubleCacheBox.WithReaderNamespace(id).Load(LastLeaveOppositeFortressTimeKey);
    }

    private void SetLastLeaveOppositeFortressTime(Camp camp, double time)
    {
        var id = camp switch
        {
            Camp.Red => BlueFortressZoneId,
            Camp.Blue => RedFortressZoneId,
            _ => throw new ArgumentOutOfRangeException()
        };

        DoubleCacheBox.WithWriterNamespace(id).Save(LastLeaveOppositeFortressTimeKey, time);
    }

    private Identity GetLastEnterOppositeFortressUser(Camp camp)
    {
        return _lastEnterOppositeFortressUser[camp];
    }

    private void SetLastEnterOppositeFortressUser(Identity user)
    {
        _lastEnterOppositeFortressUser[user.Camp] = user;
    }

    private void EnterOppositeFortressSettlementFor(Camp camp)
    {
        var outpostId = camp == Camp.Red ? Identity.BlueOutpost : Identity.RedOutpost;
        var outpost = EntitySystem.Entities[outpostId] as Outpost;
        if (TimeSystem.StageTimeElapsed < 180 || !outpost.IsDead())
            return;

        // EntitySystem.GetOperatedEntities<Infantry>(camp).Cast<IRobot>().Concat(EntitySystem.GetOperatedEntities<Sentry>(camp)).Any(r => );
        var t = GetLastEnterOppositeFortressTime(camp);
        if (t <= 0)
            return;
        var u = GetLastEnterOppositeFortressUser(camp);
        if (
            u == Identity.Spectator
            && TimeSystem.Time - GetLastLeaveOppositeFortressTime(camp) > 5
        )
        {
            SetLastEnterOppositeFortressTime(camp, -1);
            return;
        }

        if (TimeSystem.Time - t > 25)
        {
            var baseId = camp switch
            {
                Camp.Red => Identity.BlueBase,
                Camp.Blue => Identity.RedBase,
                _ => throw new ArgumentOutOfRangeException()
            };

            var b = EntitySystem.Entities[baseId] as Base;
            if (b.IsArmorOpen)
                return;

            BaseSystem.SetArmorOpen(b, true);
            CommandPublisher.PublishAsync(
                new FortressOccupyBaseEvent(camp, TimeSystem.StageTimeElapsed)
            );
        }
    }
}