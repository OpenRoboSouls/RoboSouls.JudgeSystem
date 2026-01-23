using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
    BaseSystem baseSystem,
    RM2026ucPerformanceSystem performanceSystem,
    EntitySystem entitySystem,
    BuffSystem buffSystem,
    LifeSystem lifeSystem,
    ICacheProvider<EnergyCoreSystem.EnergyCoreStatus> energyCoreStatusCacheBox,
    ICacheProvider<int> intCacheBox)
    : ISystem
{
    public enum EnergyCoreStatus: byte
    {
        /// <summary>
        /// 比赛进行中，未进入装配流程
        /// </summary>
        Idle,
        
        /// <summary>
        /// 比赛进行中，已选择装配难度，机械臂未运动到位
        /// </summary>
        Adjusting,
        
        /// <summary>
        /// 比赛进行中，已选择装配难度，机械臂已运动到位
        /// </summary>
        Step1,
        
        /// <summary>
        /// 步骤 2 完成
        /// </summary>
        Step2,
        
        /// <summary>
        /// 步骤 3 完成
        /// </summary>
        Step3,
        
        /// <summary>
        /// 步骤 4 完成
        /// </summary>
        Step4,
        
        /// <summary>
        /// 步骤 5 进行中
        /// </summary>
        Step5InProgress,
        
        /// <summary>
        /// 步骤 5 完成
        /// </summary>
        Step5,
        
        /// <summary>
        /// 已确认装配，回收能量单元中
        /// </summary>
        Resetting
    }
    
    private static int EnergyCoreStatusCacheKey => "EnergyCoreStatus".Sum();
    
    private void SetEnergyCoreStatus(Camp camp, EnergyCoreStatus status)
    {
        energyCoreStatusCacheBox.WithWriterNamespace(new Identity(camp, 0)).Save(EnergyCoreStatusCacheKey, status);
    }
    
    public EnergyCoreStatus GetEnergyCoreStatus(Camp camp)
    {
        return energyCoreStatusCacheBox.WithReaderNamespace(new Identity(camp, 0)).LoadOrDefault(EnergyCoreStatusCacheKey, EnergyCoreStatus.Idle);
    }
    
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

    /// <summary>
    /// 因装配成功而得到的每10秒金币奖励
    /// </summary>
    private readonly Dictionary<Camp, int> _coinIncr = new Dictionary<Camp, int>
    {
        [Camp.Red] = 0,
        [Camp.Blue] = 0
    };

    public Task Reset(CancellationToken cancellation = new CancellationToken())
    {
        timeSystem.RegisterRepeatAction(10, () =>
        {
            foreach (var (c, v) in _coinIncr)
            {
                economySystem.AddCoin(c, v);
            }
        });
        
        return Task.CompletedTask;
    }

    public void OnExchangeSuccess(Camp camp, int level)
    {
        if (level is < 1 or > 4)
        {
            throw new System.ArgumentOutOfRangeException(nameof(level), "Level must be between 1 and 4. Got " + level);
        }
        if (camp is not (Camp.Red or Camp.Blue))
        {
            throw new System.ArgumentOutOfRangeException(nameof(camp), "Camp must be Red or Blue. Got " + camp);
        }
        if (timeSystem.Stage != JudgeSystemStage.Match)
        {
            return;
        }
            
        var isFirst = GetAssemblyCount(camp, level) == 0;

        if (level == 1)
        {
            _coinIncr[camp] += isFirst ? 50 : 5;
        } else if (level == 2)
        {
            _coinIncr[camp] += isFirst ? 25 : 10;

            if (isFirst)
            {
                performanceSystem.SetLevelUpperBound(camp, 7);
            }
        } else if (level == 3)
        {
            _coinIncr[camp] += isFirst ? 25 : 15;
                
            if (isFirst)
            {
                performanceSystem.SetLevelUpperBound(camp, 10);
                foreach (var entity in entitySystem.GetOperatedEntities<IHealthed>(camp))
                {
                    buffSystem.AddBuff(entity.Id, RM2026ucBuffs.PermanentDefense, 0.25f, TimeSpan.MaxValue);
                }
            }
        } else 
        {
            _coinIncr[camp] += 50;
                
            foreach (var entity in entitySystem.GetOperatedEntities<IHealthed>(camp))
            {
                buffSystem.AddBuff(entity.Id, RM2026ucBuffs.PermanentDefense, 0.5f, TimeSpan.MaxValue);
            }

            baseSystem.AddHealth(camp, 2000);
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