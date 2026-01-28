using System;
using RoboSouls.JudgeSystem.Events;
using RoboSouls.JudgeSystem.Systems;
using VitalRouter;

namespace RoboSouls.JudgeSystem.RoboMaster2026UC.Systems;

/// <summary>
///     地形跨越点机制
/// </summary>
public abstract class TerrainLeapZoneSystem(
    ITimeSystem timeSystem,
    ICacheProvider<double> doubleCacheBox,
    BuffSystem buffSystem)
    : ISystem
{
    private static readonly int ActivationTimeKey = "activation_time".Sum();

    /// <summary>
    ///     触发区域ID，机器人进入此区域时开始跨越判定
    /// </summary>
    public abstract Identity TriggerZoneId { get; }

    /// <summary>
    ///     激活区域ID，机器人在此区域内完成跨越判定
    /// </summary>
    public abstract Identity ActivationZoneId { get; }

    /// <summary>
    ///     激活判定最大时间
    /// </summary>
    public abstract int MaxActivationTime { get; }

    /// <summary>
    ///     增益持续时间
    /// </summary>
    public abstract int BuffDuration { get; }

    protected abstract void OnActivationStart(in Identity operatorId);

    protected virtual void OnActivationSuccess(in Identity operatorId, double activationTime)
    {
        // 在机器人已有任意地形跨越增益时，再一次获得地形跨越增益，机器人将获得 50%的防御增益
        if (buffSystem.TryGetBuff(operatorId, RM2026ucBuffs.TerrainLeapBuff, out Buff _))
            buffSystem.AddBuff(
                operatorId,
                Buffs.DefenceBuff,
                0.5f,
                TimeSpan.FromSeconds(BuffDuration)
            );

        buffSystem.AddBuff(
            operatorId,
            RM2026ucBuffs.TerrainLeapBuff,
            1,
            TimeSpan.FromSeconds(BuffDuration)
        );
    }

    [Route]
    protected virtual void OnEnterZone(EnterZoneEvent evt)
    {
        if (evt.ZoneId == TriggerZoneId)
        {
            SetActivationTime(evt.OperatorId, timeSystem.StageTimeElapsed);
            OnActivationStart(evt.OperatorId);
        }
        else if (evt.ZoneId == ActivationZoneId)
        {
            var activationTime =
                timeSystem.StageTimeElapsed - GetActivationTime(evt.OperatorId);
            if (activationTime <= MaxActivationTime) OnActivationSuccess(evt.OperatorId, activationTime);
        }
    }

    private void SetActivationTime(in Identity operatorId, double time)
    {
        doubleCacheBox
            .WithWriterNamespace(operatorId)
            .WithWriterNamespace(TriggerZoneId)
            .Save(ActivationTimeKey, time);
    }

    private double GetActivationTime(in Identity operatorId)
    {
        return doubleCacheBox
            .WithReaderNamespace(operatorId)
            .WithReaderNamespace(TriggerZoneId)
            .Load(ActivationTimeKey);
    }
}