using System;
using System.Collections.Generic;
using System.Linq;
using RoboSouls.JudgeSystem.Entities;
using RoboSouls.JudgeSystem.RoboMaster2026UC.Entities;
using RoboSouls.JudgeSystem.RoboMaster2026UC.Events;
using RoboSouls.JudgeSystem.Systems;
using VitalRouter;

namespace RoboSouls.JudgeSystem.RoboMaster2026UC.Systems;

/// <summary>
/// 科技核心-能量单元机制
/// </summary>
public sealed class EnergyCoreSystem(
    ITimeSystem timeSystem,
    Router router,
    EconomySystem economySystem,
    RM2026ucPerformanceSystem performanceSystem,
    EntitySystem entitySystem,
    BuffSystem buffSystem,
    LifeSystem lifeSystem,
    ICacheProvider<int> intCacheBox)
    : ISystem
{
    public IEnumerable<int> GetAvailableLevels()
    {
        if (timeSystem.Stage != JudgeSystemStage.Match)
        {
            return Enumerable.Empty<int>();
        }

        return timeSystem.StageTimeElapsed switch
        {
            < 60 => new[] { 1 },
            < 120 => new[] { 1, 2 },
            < 180 => new[] { 1, 2, 3 },
            _ => new[] { 1, 2, 3, 4 }
        };
    }

    public void OnExchangeSuccess(Camp camp, int level)
    {
        if (level is < 1 or > 4)
        {
            throw new System.ArgumentOutOfRangeException(nameof(level), "Level must be between 1 and 4. Got " + level);
        }
            
        var isFirst = GetAssemblyCount(camp, level) == 0;

        if (level == 1)
        {
            var delta = isFirst ? 50 : 5;
            timeSystem.RegisterRepeatAction(10, () =>
            {
                economySystem.AddCoin(camp, delta);
            });
        } else if (level == 2)
        {
            var delta = isFirst ? 25 : 10;
            timeSystem.RegisterRepeatAction(10, () =>
            {
                economySystem.AddCoin(camp, delta);
            });

            if (isFirst)
            {
                performanceSystem.SetLevelUpperBound(camp, 7);
            }
        } else if (level == 3)
        {
            var delta = isFirst ? 25 : 15;
            timeSystem.RegisterRepeatAction(10, () =>
            {
                economySystem.AddCoin(camp, delta);
            });
                
            if (isFirst)
            {
                performanceSystem.SetLevelUpperBound(camp, 10);
                foreach (var entity in entitySystem.GetOperatedEntities<IHealthed>(camp))
                {
                    buffSystem.AddBuff(entity.Id, Buffs.DefenceBuff, 0.25f, TimeSpan.MaxValue);
                }
            }
        } else 
        {
            timeSystem.RegisterRepeatAction(10, () =>
            {
                economySystem.AddCoin(camp, 50);
            });
                
            foreach (var entity in entitySystem.GetOperatedEntities<IHealthed>(camp))
            {
                buffSystem.AddBuff(entity.Id, Buffs.DefenceBuff, 0.5f, TimeSpan.MaxValue);
            }

            lifeSystem.IncreaseHealth((Base)entitySystem.Entities[new Identity(camp, Identity.BaseId)], 2000);
        }
            
        IncrAssemblyCount(camp, level);
        router.PublishAsync(new AssemblySuccessEvent(camp, level, isFirst));
    }
        
    private const ushort FirstAssemblyCacheKey = 0x3000;

    public int GetAssemblyCount(Camp camp, int level)
    {
        return intCacheBox.WithReaderNamespace(new Identity(camp, FirstAssemblyCacheKey)).LoadOrDefault(level, 0);
    }
        
    private void IncrAssemblyCount(Camp camp, int level, int delta = 1)
    {
        var identity = new Identity(camp, FirstAssemblyCacheKey);
        var current = intCacheBox.WithReaderNamespace(identity).LoadOrDefault(level, 0);
        intCacheBox.WithWriterNamespace(identity).Save(level, current + delta);
    }
}