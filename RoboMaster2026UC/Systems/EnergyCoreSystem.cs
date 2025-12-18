using System;
using System.Collections.Generic;
using System.Linq;
using RoboSouls.JudgeSystem.Entities;
using RoboSouls.JudgeSystem.RoboMaster2026UC.Entities;
using RoboSouls.JudgeSystem.RoboMaster2026UC.Events;
using RoboSouls.JudgeSystem.Systems;
using VContainer;
using VitalRouter;

namespace RoboSouls.JudgeSystem.RoboMaster2026UC.Systems;

/// <summary>
/// 科技核心-能量单元机制
/// </summary>
public sealed class EnergyCoreSystem: ISystem
{
    [Inject] internal ITimeSystem TimeSystem { get; set; }
        
    [Inject] internal Router Router { get; set; }
        
    [Inject] internal EconomySystem EconomySystem { get; set; }
        
    [Inject] internal RM2026ucPerformanceSystem PerformanceSystem { get; set; }
        
    [Inject] internal EntitySystem EntitySystem { get; set; }
        
    [Inject] internal BuffSystem BuffSystem { get; set; }
        
    [Inject] internal LifeSystem LifeSystem { get; set; }
        
    [Inject] internal ICacheProvider<int> IntCacheBox { get; set; }

    public IEnumerable<int> GetAvailableLevels()
    {
        if (TimeSystem.Stage != JudgeSystemStage.Match)
        {
            return Enumerable.Empty<int>();
        }

        return TimeSystem.StageTimeElapsed switch
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
            TimeSystem.RegisterRepeatAction(10, () =>
            {
                EconomySystem.AddCoin(camp, delta);
            });
        } else if (level == 2)
        {
            var delta = isFirst ? 25 : 10;
            TimeSystem.RegisterRepeatAction(10, () =>
            {
                EconomySystem.AddCoin(camp, delta);
            });

            if (isFirst)
            {
                PerformanceSystem.SetLevelUpperBound(camp, 7);
            }
        } else if (level == 3)
        {
            var delta = isFirst ? 25 : 15;
            TimeSystem.RegisterRepeatAction(10, () =>
            {
                EconomySystem.AddCoin(camp, delta);
            });
                
            if (isFirst)
            {
                PerformanceSystem.SetLevelUpperBound(camp, 10);
                foreach (var entity in EntitySystem.GetOperatedEntities<IHealthed>(camp))
                {
                    BuffSystem.AddBuff(entity.Id, Buffs.DefenceBuff, 0.25f, TimeSpan.MaxValue);
                }
            }
        } else 
        {
            TimeSystem.RegisterRepeatAction(10, () =>
            {
                EconomySystem.AddCoin(camp, 50);
            });
                
            foreach (var entity in EntitySystem.GetOperatedEntities<IHealthed>(camp))
            {
                BuffSystem.AddBuff(entity.Id, Buffs.DefenceBuff, 0.5f, TimeSpan.MaxValue);
            }

            LifeSystem.IncreaseHealth((Base)EntitySystem.Entities[new Identity(camp, Identity.BaseId)], 2000);
        }
            
        IncrAssemblyCount(camp, level);
        Router.PublishAsync(new AssemblySuccessEvent(camp, level, isFirst));
    }
        
    private const ushort FirstAssemblyCacheKey = 0x3000;

    public int GetAssemblyCount(Camp camp, int level)
    {
        return IntCacheBox.WithReaderNamespace(new Identity(camp, FirstAssemblyCacheKey)).LoadOrDefault(level, 0);
    }
        
    private void IncrAssemblyCount(Camp camp, int level, int delta = 1)
    {
        var identity = new Identity(camp, FirstAssemblyCacheKey);
        var current = IntCacheBox.WithReaderNamespace(identity).LoadOrDefault(level, 0);
        IntCacheBox.WithWriterNamespace(identity).Save(level, current + delta);
    }
}