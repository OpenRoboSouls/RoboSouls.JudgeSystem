using System.Threading;
using System.Threading.Tasks;
using RoboSouls.JudgeSystem.Events;
using RoboSouls.JudgeSystem.RoboMaster2024UL.Entities;
using RoboSouls.JudgeSystem.Systems;
using VContainer;
using VitalRouter;

namespace RoboSouls.JudgeSystem.RoboMaster2024UL.Systems;

/// <summary>
/// 基地机制
/// </summary>
[Routes]
public sealed partial class BaseSystem : ISystem
{
    [Inject]
    internal void Inject(Router router)
    {
        MapTo(router);
    }

    [Inject]
    internal ICacheWriter<uint> UintCacheBox { get; set; }

    [Inject]
    internal LifeSystem LifeSystem { get; set; }

    [Inject]
    internal EntitySystem EntitySystem { get; set; }

    [Inject]
    internal ITimeSystem TimeSystem { get; set; }

    public Task Reset(CancellationToken cancellation = new CancellationToken())
    {
        var redBase = EntitySystem.Entities[Identity.RedBase] as Base;
        var blueBase = EntitySystem.Entities[Identity.BlueBase] as Base;

        SetShield(redBase, RM2024ulPerformanceSystem.GetBaseMaxShield);
        SetShield(blueBase, RM2024ulPerformanceSystem.GetBaseMaxShield);
        LifeSystem.SetInvincible(redBase, true);
        LifeSystem.SetInvincible(blueBase, true);

        TimeSystem.RegisterOnceAction(JudgeSystemStage.Match, 60, CheckSentryAbsent);

        return Task.CompletedTask;
    }

    private void SetShield(Base b, uint shield)
    {
        UintCacheBox.WithWriterNamespace(b.Id).Save(Base.ShieldCacheKey, shield);
    }

    /// <summary>
    /// 减少护盾值
    /// </summary>
    /// <param name="b"></param>
    /// <param name="delta"></param>
    /// <returns>实际减少的护盾值</returns>
    public uint DecreaseShield(Base b, uint delta)
    {
        var shield = b.Shield;
        if (delta > shield)
        {
            delta = shield;
        }

        var newShield = shield - delta;
        SetShield(b, newShield);

        return delta;
    }

    /// <summary>
    /// 比赛过程中， 一方出现首次机器人战亡或被罚下时，该方基地的无敌状态解除， 基地虚拟护盾生效，
    /// 若一方哨兵机器人战亡或被罚下， 该方基地无敌状态和虚拟护盾均失效。
    /// </summary>
    /// <param name="evt"></param>
    [Route]
    private void OnKill(KillEvent evt)
    {
        if (!evt.Victim.IsRobotCamp())
            return;

        Base b;
        if (evt.Victim.Camp == Camp.Red)
        {
            b = EntitySystem.Entities[Identity.RedBase] as Base;
        }
        else
        {
            b = EntitySystem.Entities[Identity.BlueBase] as Base;
        }

        RemoveInvincible(b);

        if (evt.Victim.IsSentry())
            RemoveShield(b);
    }

    private void RemoveInvincible(Base b)
    {
        if (!LifeSystem.IsInvincible(b))
            return;

        LifeSystem.SetInvincible(b, false);
        SetShield(b, RM2024ulPerformanceSystem.GetBaseMaxShield);
    }

    private void RemoveShield(Base b)
    {
        RemoveInvincible(b);
        SetShield(b, 0);
    }

    /// <summary>
    /// 若一方哨兵机器人未上场，则比赛开始 1 分钟后，该方基地无敌状态和虚拟护盾均失效。
    /// </summary>
    /// <returns></returns>
    private Task CheckSentryAbsent()
    {
        var redBase = EntitySystem.Entities[Identity.RedBase] as Base;
        var blueBase = EntitySystem.Entities[Identity.BlueBase] as Base;

        if (!EntitySystem.HasOperator(Identity.RedSentry))
        {
            RemoveShield(redBase);
        }

        if (!EntitySystem.HasOperator(Identity.BlueSentry))
        {
            RemoveShield(blueBase);
        }

        return Task.CompletedTask;
    }
}