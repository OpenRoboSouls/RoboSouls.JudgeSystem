using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RoboSouls.JudgeSystem.Entities;
using RoboSouls.JudgeSystem.Systems;

namespace RoboSouls.JudgeSystem.RoboMaster2026UC.Systems;

/// <summary>
/// 雷达机制
/// </summary>
public sealed class RadarSystem(
    ITimeSystem timeSystem,
    ICacheProvider<float> floatCacheBox,
    IRadarBrain radarBrain,
    BuffSystem buffSystem,
    ICacheProvider<int> intCacheBox,
    EntitySystem entitySystem)
    : ISystem
{
    public Task Reset(CancellationToken cancellation = new CancellationToken())
    {
        timeSystem.RegisterRepeatAction(0.5f, RadarScanTask);
        _lastMark.Clear();

        return Task.CompletedTask;
    }

    private Task RadarScanTask()
    {
        if (timeSystem.Stage != JudgeSystemStage.Match)
            return Task.CompletedTask;

        return Task.WhenAll(
            RadarScanUpdateTaskFor(Identity.RedHero),
            RadarScanUpdateTaskFor(Identity.RedEngineer),
            RadarScanUpdateTaskFor(Identity.RedInfantry1),
            RadarScanUpdateTaskFor(Identity.RedInfantry2),
            RadarScanUpdateTaskFor(Identity.RedSentry),
            RadarScanUpdateTaskFor(Identity.BlueHero),
            RadarScanUpdateTaskFor(Identity.BlueEngineer),
            RadarScanUpdateTaskFor(Identity.BlueInfantry1),
            RadarScanUpdateTaskFor(Identity.BlueInfantry2),
            RadarScanUpdateTaskFor(Identity.BlueSentry)
        );
    }

    private readonly Dictionary<Identity, Mark> _lastMark = new();

    private Task RadarScanUpdateTaskFor(in Identity id)
    {
        var mark = radarBrain.OnRadarScan(id);
        var lastMark = _lastMark.GetValueOrDefault(id, Mark.Wrong);
        var x = GetMarkProgressDelta(id);

        switch (mark)
        {
            case Mark.Accurate:
                if (lastMark is Mark.Accurate or Mark.SemiAccurate)
                {
                    x += 1f;
                }
                else
                {
                    x = 1f;
                }

                break;
            case Mark.SemiAccurate:
                if (lastMark is Mark.Accurate or Mark.SemiAccurate)
                {
                    x += 0.5f;
                }
                else
                {
                    x = 0.5f;
                }

                break;
            case Mark.Wrong:
                if (lastMark is Mark.Accurate or Mark.SemiAccurate)
                {
                    x = -0.8f;
                }
                else
                {
                    x -= 0.8f;
                }

                break;
        }

        _lastMark[id] = mark;
        SetMarkProgress(id, GetMarkProgress(id) + x);
        SetMarkProgressDelta(id, x);

        return Task.CompletedTask;
    }

    private static readonly int MarkProgressCacheKey = "MarkProgress".Sum();
    private static readonly int MarkProgressDeltaCacheKey = "MarkProgressDelta".Sum();

    public float GetMarkProgress(in Identity id)
    {
        return floatCacheBox.WithReaderNamespace(id).Load(MarkProgressCacheKey);
    }

    private void SetMarkProgress(in Identity id, float progress)
    {
        progress = Math.Clamp(progress, 0f, 120f);

        if (progress >= 100f)
        {
            // 易伤效果
            if (!buffSystem.TryGetBuff(id, RM2026ucBuffs.Vulnerable, out Buff buff))
            {
                buffSystem.AddBuff(id, RM2026ucBuffs.Vulnerable, 0.15f, TimeSpan.MaxValue);
            }
            else if (timeSystem.Time - buff.Value > 60)
            {
                // 持续60秒添加双倍易伤机会
                SetDoubleVulnerableChanceGained(
                    id.Camp,
                    GetDoubleVulnerableChanceGained(id.Camp) + 1
                );
            }
        }
        else
        {
            buffSystem.RemoveBuff(id, RM2026ucBuffs.Vulnerable);
        }

        floatCacheBox.WithWriterNamespace(id).Save(MarkProgressCacheKey, progress);
    }

    private float GetMarkProgressDelta(in Identity id)
    {
        return floatCacheBox.WithReaderNamespace(id).Load(MarkProgressDeltaCacheKey);
    }

    private void SetMarkProgressDelta(in Identity id, float delta)
    {
        floatCacheBox.WithWriterNamespace(id).Save(MarkProgressDeltaCacheKey, delta);
    }

    private static readonly int DoubleVulnerableChanceGainCacheKey =
        "DoubleVulnerableChanceGain".Sum();
    private static readonly int DoubleVulnerableChanceConsumedCacheKey =
        "DoubleVulnerableChanceConsumed".Sum();

    private int GetDoubleVulnerableChanceGained(Camp camp)
    {
        return intCacheBox
            .WithReaderNamespace(new Identity(camp, 0))
            .Load(DoubleVulnerableChanceGainCacheKey);
    }

    private void SetDoubleVulnerableChanceGained(Camp camp, int gain)
    {
        intCacheBox
            .WithWriterNamespace(new Identity(camp, 0))
            .Save(DoubleVulnerableChanceGainCacheKey, gain);
    }

    private int GetDoubleVulnerableChanceConsumed(Camp camp)
    {
        return intCacheBox
            .WithReaderNamespace(new Identity(camp, 0))
            .Load(DoubleVulnerableChanceConsumedCacheKey);
    }

    private void SetDoubleVulnerableChanceConsumed(Camp camp, int consumed)
    {
        intCacheBox
            .WithWriterNamespace(new Identity(camp, 0))
            .Save(DoubleVulnerableChanceConsumedCacheKey, consumed);
    }

    /// <summary>
    /// 剩余双倍易伤机会
    /// </summary>
    /// <param name="camp"></param>
    /// <returns></returns>
    public int GetDoubleVulnerableChanceRemaining(Camp camp)
    {
        var gained = Math.Min(
            GetDoubleVulnerableChanceGained(camp),
            RM2026ucPerformanceSystem.MaxDoubleVulnerableChange
        );
        var consumed = GetDoubleVulnerableChanceConsumed(camp);

        return gained - consumed;
    }

    /// <summary>
    /// 发动双倍易伤
    /// </summary>
    /// <param name="camp"></param>
    /// <returns></returns>
    public bool TryStartDoubleVulnerableChance(Camp camp)
    {
        var remaining = GetDoubleVulnerableChanceRemaining(camp);
        if (remaining <= 0)
            return false;

        SetDoubleVulnerableChanceConsumed(camp, GetDoubleVulnerableChanceConsumed(camp) + 1);

        var targets = entitySystem
            .GetOperatedEntities<IHealthed>(camp.GetOppositeCamp())
            .Where(h => buffSystem.TryGetBuff(h.Id, RM2026ucBuffs.Vulnerable, out Buff _));

        foreach (var healthed in targets)
        {
            buffSystem.AddBuff(
                healthed.Id,
                RM2026ucBuffs.Vulnerable,
                0.3f,
                TimeSpan.FromSeconds(30)
            );
        }

        return true;
    }
}

public interface IRadarBrain
{
    public Mark OnRadarScan(in Identity id);
}

public enum Mark : byte
{
    // 准确
    Accurate,

    // 半准确
    SemiAccurate,

    // 错误
    Wrong,
}