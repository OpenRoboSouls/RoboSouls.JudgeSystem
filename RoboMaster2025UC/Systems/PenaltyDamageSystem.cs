using System;
using System.Linq;
using RoboSouls.JudgeSystem.Entities;
using RoboSouls.JudgeSystem.Events;
using RoboSouls.JudgeSystem.Systems;
using VContainer;
using VitalRouter;

namespace RoboSouls.JudgeSystem.RoboMaster2025UC.Systems;

[Routes]
public sealed partial class PenaltyDamageSystem : ISystem
{
    [Inject]
    internal void Inject(Router router)
    {
        MapTo(router);
    }

    [Inject]
    internal ITimeSystem TimeSystem { get; set; }

    [Inject]
    internal LifeSystem LifeSystem { get; set; }

    [Inject]
    internal EntitySystem EntitySystem { get; set; }

    [Inject]
    internal PerformanceSystemBase PerformanceSystem { get; set; }

    /// <summary>
    /// 裁判系统自动扣除违规机器人当前上限血量的 15%，其余存活机器人被扣
    /// 除当前上限血量的 5%。 机器人每次收到黄牌警告后的 30 秒内，若再次收
    ///     到黄牌警告，则扣除当前上限血量的百分比是前一次的 2 倍，其余存活机
    ///     器人被扣除当前上限血量的 5%。
    /// </summary>
    /// <param name="evt"></param>
    [Route]
    private void OnPenalty(JudgePenaltyEvent evt)
    {
        if (TimeSystem.Stage is not JudgeSystemStage.Match)
            return;

        if (
            EntitySystem.TryGetOperatedEntity(evt.TargetId, out IHealthed healthed)
            && healthed.Health > 1
        )
        {
            var decrease = Math.Min(
                PerformanceSystem.GetMaxHealth(healthed) * 0.15,
                healthed.Health - 1
            );
            LifeSystem.DecreaseHealth(healthed, evt.JudgeId, (uint)decrease);
        }

        foreach (
            var h in EntitySystem
                .GetOperatedEntities<IHealthed>(evt.TargetId.Camp)
                .Where(i => i.Id != evt.TargetId)
        )
        {
            var decrease = Math.Min(PerformanceSystem.GetMaxHealth(h) * 0.05, h.Health - 1);
            LifeSystem.DecreaseHealth(h, evt.JudgeId, (uint)decrease);
        }
    }
}