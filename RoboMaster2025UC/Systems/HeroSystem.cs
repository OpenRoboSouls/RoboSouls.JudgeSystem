using System;
using RoboSouls.JudgeSystem.Entities;
using RoboSouls.JudgeSystem.RoboMaster2025UC.Entities;
using RoboSouls.JudgeSystem.RoboMaster2025UC.Events;
using RoboSouls.JudgeSystem.Systems;
using VContainer;
using VitalRouter;

namespace RoboSouls.JudgeSystem.RoboMaster2025UC.Systems;

/// <summary>
/// 英雄机器人机制
///
/// 英雄机器人位于己方半场（详见“图 5-20 己方半场示意图” ） 时，可以选择进入“部署模式”。 确认进
/// 入“部署模式” 2 秒后，英雄机器人进入“部署模式”， 在该模式下， 英雄机器人底盘断电，获得 25%防
/// 御增益，且发射 42mm 弹丸攻击基地时具有 150%攻击增益，在命中基地后， 己方获得 50 金币。
/// 在“部署模式”下攻击基地时，具有攻击增益和金币奖励的 42mm 弹丸数量受限。在比赛进行 t 秒后，具
/// 有攻击增益和金币奖励的弹丸数量至多为 M。 M 与 t 的关系为M = 2 + 𝑡𝑡
/// 20，计算结果向下取整。在部署模
/// 式下，命中对方基地的具有增益的 42mm 弹丸总数量=M 后，英雄机器人发射 42mm 弹丸攻击基地不再
/// 具有 150%攻击增益和金币奖励； 在部署模式下命中对方基地的具有增益的 42mm 弹丸总数量＜M 时，
/// 英雄机器人发射 42mm 弹丸攻击基地可重新获得上述增益和金币奖励。
/// </summary>
public sealed class HeroSystem : ISystem
{
    public const ushort DeploymentZoneId = 250;
    public static readonly Identity RedDeployZoneId = new Identity(Camp.Red, DeploymentZoneId);
    public static readonly Identity BlueDeployZoneId = new Identity(
        Camp.Blue,
        DeploymentZoneId
    );

    private static readonly int DeployHitCountCacheKey = "deploy_hit_count".Sum();
    private static readonly int DeployStartTimeCacheKey = "deploy_start_time".Sum();

    [Inject]
    internal BuffSystem BuffSystem { get; set; }

    [Inject]
    internal ICommandPublisher Publisher { get; set; }

    [Inject]
    internal ICacheWriter<bool> BoolCacheBox { get; set; }

    [Inject]
    internal ICacheProvider<int> IntCacheBox { get; set; }

    [Inject]
    internal ICacheProvider<double> DoubleCacheBox { get; set; }

    [Inject]
    internal EntitySystem EntitySystem { get; set; }

    [Inject]
    internal ZoneSystem ZoneSystem { get; set; }

    [Inject]
    internal ITimeSystem TimeSystem { get; set; }

    [Inject]
    internal ICommandPublisher CommandPublisher { get; set; }

    public enum EnterDeploymentModeRefuseReason
    {
        Dead,
        Zone,
        Unknown,
    }

    public bool CanEnterDeploymentMode(
        in Identity id,
        out EnterDeploymentModeRefuseReason reason
    )
    {
        reason = EnterDeploymentModeRefuseReason.Unknown;
        if (!id.IsHero())
            return false;

        if (TimeSystem.Stage != JudgeSystemStage.Match)
            return false;

        if (!EntitySystem.TryGetOperatedEntity(id, out Hero h) || h.IsDead())
        {
            reason = EnterDeploymentModeRefuseReason.Dead;
            return false;
        }

        if (!ZoneSystem.IsInZone(id, new Identity(id.Camp, DeploymentZoneId)))
        {
            reason = EnterDeploymentModeRefuseReason.Zone;
            return false;
        }

        return true;
    }

    public void EnterDeploymentMode(in Identity id)
    {
        if (!CanEnterDeploymentMode(id, out _))
            return;

        SetDeploymentMode(id, true);
    }

    public void ExitDeploymentMode(in Identity id)
    {
        if (!id.IsHero())
            return;

        if (!EntitySystem.TryGetOperatedEntity(id, out Hero hero))
            return;

        SetDeploymentMode(id, false);
    }

    /// <summary>
    /// 在比赛进行 t 秒后，具有攻击增益和金币奖励的弹丸数量至多为 M。 M 与 t 的关系为M = 2 + 𝑡/20，
    /// </summary>
    /// <returns></returns>
    public int GetDeployHitCountAllowance()
    {
        if (TimeSystem.Stage != JudgeSystemStage.Match)
            return 0;

        var t = TimeSystem.StageTimeElapsed;
        var m = 2 + (int)Math.Round(t / 20);
        return m;
    }

    public int GetDeployHitCount(Camp camp)
    {
        var id = camp == Camp.Red ? RedDeployZoneId : BlueDeployZoneId;

        return IntCacheBox.WithReaderNamespace(id).Load(DeployHitCountCacheKey);
    }

    public bool CanDeploymentHit(in Identity id)
    {
        return IsDeploymentMode(id)
               && GetDeployHitCount(id.Camp) < GetDeployHitCountAllowance();
    }

    public bool IsDeploymentMode(in Identity id)
    {
        if (!id.IsHero())
            return false;

        return !TryGetDeployProgress(id, out _)
               && BuffSystem.TryGetBuff(id, RM2025ucBuffs.HeroDeploymentModeBuff, out Buff _);
    }

    private void SetDeployHitCount(Camp camp, int count)
    {
        var id = camp == Camp.Red ? RedDeployZoneId : BlueDeployZoneId;

        IntCacheBox.WithWriterNamespace(id).Save(DeployHitCountCacheKey, count);
    }

    private void SetDeploymentMode(in Identity id, bool isDeploymentMode)
    {
        if (isDeploymentMode)
        {
            SetDeployModeEnterTime(id, TimeSystem.Time);
            BuffSystem.AddBuff(id, RM2025ucBuffs.HeroDeploymentModeBuff, 1, TimeSpan.MaxValue);
            // BuffSystem.AddBuff(id, Buffs.AttackBuff, 1.5f, TimeSpan.MaxValue);
            BuffSystem.AddBuff(id, Buffs.DefenceBuff, 0.25f, TimeSpan.MaxValue);
            Publisher.PublishAsync(new HeroEnterDeploymentModeEvent(id));
        }
        else
        {
            BuffSystem.RemoveBuff(id, RM2025ucBuffs.HeroDeploymentModeBuff);
            // BuffSystem.RemoveBuff(id, Buffs.AttackBuff);
            BuffSystem.RemoveBuff(id, Buffs.DefenceBuff);
            Publisher.PublishAsync(new HeroExitDeploymentModeEvent(id));
        }
    }

    public bool TryGetDeployProgress(in Identity id, out double progress)
    {
        var elapsed = TimeSystem.Time - GetDeployModeEnterTime(id);
        progress = elapsed / RM2025ucPerformanceSystem.DeploymentEnterTime;
        return progress <= 1;
    }

    private double GetDeployModeEnterTime(in Identity id)
    {
        return DoubleCacheBox.WithReaderNamespace(id).Load(DeployStartTimeCacheKey);
    }

    private void SetDeployModeEnterTime(in Identity id, double time)
    {
        DoubleCacheBox.WithWriterNamespace(id).Save(DeployStartTimeCacheKey, time);
    }

    internal void OnDeploymentHit(in Identity id)
    {
        var count = GetDeployHitCount(id.Camp);
        SetDeployHitCount(id.Camp, count + 1);

        // EconomySystem.SetCoin(id.Camp, EconomySystem.GetCoin(id.Camp) + 50);

        CommandPublisher.PublishAsync(
            new DeployHitEvent(
                id.Camp,
                count + 1,
                GetDeployHitCountAllowance(),
                TimeSystem.StageTimeElapsed
            )
        );
    }
}