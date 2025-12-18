using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RoboSouls.JudgeSystem.Entities;
using RoboSouls.JudgeSystem.RoboMaster2025UC.Entities;
using RoboSouls.JudgeSystem.RoboMaster2025UC.Events;
using RoboSouls.JudgeSystem.Systems;
using VContainer;

namespace RoboSouls.JudgeSystem.RoboMaster2025UC.Systems;

public sealed class RM2025ucLifeSystem : LifeSystem
{
    private static readonly int ReviveProgressTotalCacheKey = "revive_progress_total".Sum();
    private static readonly int ReviveProgressRemainingCacheKey =
        "revive_progress_remaining".Sum();
    private static readonly int BuyReviveCountCacheKey = "buy_revive_count".Sum();

    [Inject]
    internal ZoneSystem ZoneSystem { get; set; }

    [Inject]
    internal EconomySystem EconomySystem { get; set; }

    public override async Task Reset(CancellationToken cancellation = new CancellationToken())
    {
        await base.Reset(cancellation);
        TimeSystem.RegisterRepeatAction(
            1,
            () =>
                Task.WhenAll(
                    EntitySystem
                        .Entities.Values.OfType<IRobot>()
                        .OfType<IHealthed>()
                        .Where(e => EntitySystem.HasOperator(e.Id))
                        .Select(h =>
                        {
                            ReviveProgressNaturalIncrease(h);
                            return Task.CompletedTask;
                        })
                )
        );
    }

    public bool TryBuyRevive(Identity id)
    {
        if (!EntitySystem.TryGetOperatedEntity(id, out IHealthed h))
        {
            return false;
        }

        if (!h.IsDead())
            return false;
        if (!EntitySystem.HasOperator(h.Id))
            return false;
        if (h is not IRobot)
            return false;
        if (BuffSystem.TryGetBuff(h.Id, Buffs.RedCard, out Buff _))
            return false;

        var cost = CalcBuyReviveRequiredCoin(id);
        if (id.Camp == Camp.Red)
        {
            if (EconomySystem.RedCoin < cost)
                return false;
            EconomySystem.RedCoin -= cost;
        }
        else if (id.Camp == Camp.Blue)
        {
            if (EconomySystem.BlueCoin < cost)
                return false;
            EconomySystem.BlueCoin -= cost;
        }
        else
        {
            return false;
        }

        /*
         * 当通过使用金币兑换立即复活时：
 保持战亡前的等级与经验值，暂时处于无敌状态，持续 3 秒
 血量恢复至上限血量的 100%
 发射机构立即解锁
 底盘功率上限提高 1 倍（但不超过 200W），持续 4 秒
         */
        SetHealth(h, PerformanceSystem.GetMaxHealth(h));
        BuffSystem.AddBuff(h.Id, Buffs.DefenceBuff, 1, TimeSpan.FromSeconds(3));
        BuffSystem.AddBuff(h.Id, Buffs.PowerBuff, 2, TimeSpan.FromSeconds(4));

        SetBuyReviveCount(id, GetBuyReviveCount(id) + 1);

        Logger.Info($"Buy revive {id} cost {cost}");
        Publisher.PublishAsync(new BuyReviveEvent(id, cost, TimeSystem.StageTimeElapsed));

        return true;
    }

    public override bool TryRevive(in Identity healthed)
    {
        if (!base.TryRevive(healthed))
            return false;

        if (GetRemainingReviveRequiredProgress(healthed) > 0)
            return false;

        return true;
    }

    protected override void OnRevive(IHealthed healthed)
    {
        // “血量恢复至上限血量的10%，复活后处于无敌状态，持续时间10秒”
        SetHealth(healthed, (uint)(PerformanceSystem.GetMaxHealth(healthed) * 0.1f));
        BuffSystem.AddBuff(healthed.Id, Buffs.DefenceBuff, 1, TimeSpan.FromSeconds(10));

        base.OnRevive(healthed);
    }

    protected override void OnKill(IHealthed healthed, in Identity killer)
    {
        base.OnKill(healthed, killer);

        var progress = CalcReviveRequiredProgress(healthed.Id);
        SetTotalReviveRequiredProgress(healthed.Id, progress);
        SetRemainingReviveRequiredProgress(healthed.Id, progress);
    }

    private int CalcReviveRequiredProgress(Identity healthed)
    {
        var buyCount = GetBuyReviveCount(healthed);
        return (int)Math.Round(10d + TimeSystem.StageTimeElapsed / 10d + 20d * buyCount);
    }

    public int CalcBuyReviveRequiredCoin(Identity id)
    {
        /*
         * ROUNDUP(
420−比赛剩余时长
60
) × 80 + 机器人等级 × 20
金币/1 台
         */
        var level = 1;
        if (!EntitySystem.TryGetOperatedEntity(id, out IEntity e))
            return 0;
        if (e is IExperienced experienced)
            level = PerformanceSystem.GetLevel(experienced);
        else if (e is Engineer)
            level = 1;
        else if (e is Sentry)
            level = 10;

        return (int)(Math.Round(TimeSystem.StageTimeElapsed / 60d) * 80 + level * 20);
    }

    public int GetTotalReviveRequiredProgress(in Identity healthed)
    {
        return IntCacheBox.WithReaderNamespace(healthed).Load(ReviveProgressTotalCacheKey);
    }

    public int GetTotalReviveRequiredSeconds(in Identity healthed)
    {
        return GetTotalReviveRequiredProgress(healthed) * 1;
    }

    private void SetTotalReviveRequiredProgress(in Identity healthed, int value)
    {
        IntCacheBox.WithWriterNamespace(healthed).Save(ReviveProgressTotalCacheKey, value);
    }

    public int GetRemainingReviveRequiredProgress(in Identity healthed)
    {
        return IntCacheBox.WithReaderNamespace(healthed).Load(ReviveProgressRemainingCacheKey);
    }

    public int GetRemainingReviveRequiredSeconds(in Identity healthed)
    {
        return GetRemainingReviveRequiredProgress(healthed) * 1;
    }

    private void SetRemainingReviveRequiredProgress(in Identity healthed, int value)
    {
        IntCacheBox.WithWriterNamespace(healthed).Save(ReviveProgressRemainingCacheKey, value);
    }

    private void ReviveProgressNaturalIncrease(IHealthed healthed)
    {
        if (!healthed.IsDead())
            return;

        var remaining = GetRemainingReviveRequiredProgress(healthed.Id);
        if (remaining <= 0)
            return;

        var baseId = healthed.Id.Camp switch
        {
            Camp.Red => Identity.RedBase,
            Camp.Blue => Identity.BlueBase,
            _ => throw new ArgumentOutOfRangeException(),
        };
        var b = EntitySystem.Entities[baseId] as Base;
        var delta = 1;
        if (SupplySystem.IsInSupplyZone(ZoneSystem, healthed.Id))
        {
            delta = 4;
        }

        if (b.IsArmorOpen)
        {
            delta = 4;
        }
        SetRemainingReviveRequiredProgress(healthed.Id, Math.Max(0, remaining - delta));
    }

    public int GetBuyReviveCount(Identity id)
    {
        return IntCacheBox.WithReaderNamespace(id).Load(BuyReviveCountCacheKey);
    }

    private void SetBuyReviveCount(Identity id, int value)
    {
        IntCacheBox.WithWriterNamespace(id).Save(BuyReviveCountCacheKey, value);
    }
}