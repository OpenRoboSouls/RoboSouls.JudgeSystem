using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RoboSouls.JudgeSystem.Entities;
using RoboSouls.JudgeSystem.RoboMaster2024UL.Entities;
using RoboSouls.JudgeSystem.Systems;

namespace RoboSouls.JudgeSystem.RoboMaster2024UL.Systems;

public sealed class RM2024ulLifeSystem : LifeSystem
{
    private static readonly int ReviveProgressCacheKey = "revive_progress".Sum();

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

    protected override void OnRevive(IHealthed healthed)
    {
        if (!healthed.IsDead())
            return;
        if (!EntitySystem.HasOperator(healthed.Id))
            return;
        if (healthed is not IRobot)
            return;
        if (
            GetReviveRequiredProgress(healthed)
            > IntCacheBox.WithReaderNamespace(healthed.Id).Load(ReviveProgressCacheKey)
        )
            return;

        // “血量恢复至上限血量的20%，复活后处于无敌状态，持续时间10秒”
        SetHealth(healthed, (uint)(PerformanceSystem.GetMaxHealth(healthed) * 0.2));
        SetInvincible(healthed, true);
        TimeSystem.RegisterOnceAction(10, () => SetInvincible(healthed, false));

        base.OnRevive(healthed);
    }

    protected override void OnKill(IHealthed healthed, in Identity killer)
    {
        base.OnKill(healthed, killer);

        IntCacheBox.WithWriterNamespace(healthed.Id).Save(ReviveProgressCacheKey, 0);
    }

    private int GetReviveRequiredProgress(IHealthed healthed)
    {
        if (healthed is Hero)
            return 20;
        if (healthed is Infantry)
            return 10;
        return int.MaxValue;
    }

    /// <summary>
    /// 获取复活进度
    /// </summary>
    /// <param name="healthed"></param>
    /// <param name="value"></param>
    /// <param name="required"></param>
    /// <returns>是否处于死亡状态</returns>
    public bool TryGetReviveProgress(IHealthed healthed, out int value, out int required)
    {
        value = IntCacheBox.WithReaderNamespace(healthed.Id).Load(ReviveProgressCacheKey);
        required = GetReviveRequiredProgress(healthed);

        return healthed.IsDead();
    }

    /// <summary>
    /// 每秒自动增加2点复活进度
    /// </summary>
    /// <param name="progress"></param>
    /// <returns>所需秒数</returns>
    public static int GetReviveProgressRequiredTime(int progress)
    {
        return 2 * progress;
    }

    private void ReviveProgressNaturalIncrease(IHealthed healthed)
    {
        if (!healthed.IsDead())
            return;

        var current = IntCacheBox.WithReaderNamespace(healthed.Id).Load(ReviveProgressCacheKey);
        var required = GetReviveRequiredProgress(healthed);
        if (current >= required)
            return;

        IntCacheBox
            .WithWriterNamespace(healthed.Id)
            .Save(ReviveProgressCacheKey, current + GetReviveProgressRequiredTime(1));
    }
}