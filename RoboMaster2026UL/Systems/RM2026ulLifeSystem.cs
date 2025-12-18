using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RoboSouls.JudgeSystem.Entities;
using RoboSouls.JudgeSystem.Systems;

namespace RoboSouls.JudgeSystem.RoboMaster2026UL.Systems;

public sealed class RM2026ulLifeSystem : LifeSystem
{
    private static readonly int ReviveProgressTotalCacheKey = "revive_progress_total".Sum();
    private static readonly int ReviveProgressRemainingCacheKey =
        "revive_progress_remaining".Sum();
    private static readonly int DeathCountCacheKey = "death_count".Sum();

    public override async Task Reset(CancellationToken cancellation = new CancellationToken())
    {
        await base.Reset(cancellation);
        TimeSystem.RegisterRepeatAction(
            1,
            async () =>
            {
                await Task.WhenAll(
                    EntitySystem
                        .Entities.Values.OfType<IRobot>()
                        .OfType<IHealthed>()
                        .Where(e => EntitySystem.HasOperator(e.Id))
                        .Select(h =>
                        {
                            ReviveProgressNaturalIncrease(h);
                            return Task.CompletedTask;
                        })
                );
            }
        );
    }

    public override bool TryRevive(in Identity healthed)
    {
        if (!base.TryRevive(in healthed))
            return false;

        if (GetRemainingReviveRequiredProgress(healthed) > 0)
            return false;

        return true;
    }

    protected override void OnRevive(IHealthed healthed)
    {
        // “血量恢复至上限血量的20%，复活后处于无敌状态，持续时间10秒”
        SetHealth(healthed, (uint)(PerformanceSystem.GetMaxHealth(healthed) * 0.2f));
        BuffSystem.AddBuff(healthed.Id, Buffs.DefenceBuff, 1, TimeSpan.FromSeconds(10));

        base.OnRevive(healthed);
    }

    protected override void OnKill(IHealthed healthed, in Identity killer)
    {
        base.OnKill(healthed, killer);

        var progress = CalcReviveRequiredProgress(healthed.Id);
        SetTotalReviveRequiredProgress(healthed.Id, progress);
        SetRemainingReviveRequiredProgress(healthed.Id, progress);
        SetDeathCount(healthed.Id, GetDeathCount(healthed.Id) + 1);
    }

    public int GetTotalReviveRequiredSeconds(in Identity healthed)
    {
        return GetTotalReviveRequiredProgress(healthed) * 1;
    }

    private int CalcReviveRequiredProgress(in Identity healthed)
    {
        return (GetDeathCount(healthed) + 1) * 10;
    }

    public int GetDeathCount(in Identity id)
    {
        return IntCacheBox.WithReaderNamespace(id).Load(DeathCountCacheKey);
    }

    private void SetDeathCount(in Identity id, int value)
    {
        IntCacheBox.WithWriterNamespace(id).Save(DeathCountCacheKey, value);
    }

    public int GetTotalReviveRequiredProgress(in Identity healthed)
    {
        return IntCacheBox.WithReaderNamespace(healthed).Load(ReviveProgressTotalCacheKey);
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

        const int delta = 2;
        SetRemainingReviveRequiredProgress(healthed.Id, Math.Max(0, remaining - delta));
    }
}