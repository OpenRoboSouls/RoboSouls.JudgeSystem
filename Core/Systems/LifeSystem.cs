using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RoboSouls.JudgeSystem.Entities;
using RoboSouls.JudgeSystem.Events;
using VContainer;
using VitalRouter;

namespace RoboSouls.JudgeSystem.Systems;

public abstract class LifeSystem : ISystem
{
    public const ushort KillReasonOverheat = 300;

    [Inject] protected ICommandPublisher Publisher { get; set; }

    [Inject] protected EntitySystem EntitySystem { get; set; }

    [Inject] protected PerformanceSystemBase PerformanceSystem { get; set; }

    [Inject] protected ICacheProvider<int> IntCacheBox { get; set; }

    [Inject] protected ICacheWriter<uint> UIntCacheBox { get; set; }

    [Inject] protected BuffSystem BuffSystem { get; set; }

    [Inject] protected ITimeSystem TimeSystem { get; set; }

    [Inject] protected ILogger Logger { get; set; }

    public virtual Task Reset(CancellationToken cancellation = new())
    {
        return Task.WhenAll(
            EntitySystem
                .Entities.Values.OfType<IHealthed>()
                .Select(e =>
                {
                    if (EntitySystem.HasOperator(e.Id) || e is IBuilding)
                        ResetHealth(e);
                    else
                        DecreaseHealth(e, Identity.Server, int.MaxValue);

                    return Task.CompletedTask;
                })
        );
    }

    protected virtual void OnKill(IHealthed healthed, in Identity killer)
    {
        Logger.Info($"Kill {healthed.Id} by {killer}");
        Publisher.PublishAsync(new KillEvent(TimeSystem.Time, killer, healthed.Id));
    }

    /// <summary>
    ///     尝试操作复活
    /// </summary>
    /// <param name="healthed"></param>
    /// <returns></returns>
    public virtual bool TryRevive(in Identity healthed)
    {
        if (!EntitySystem.TryGetOperatedEntity(healthed, out IHealthed h)) return false;

        if (!h.IsDead())
            return false;
        if (!EntitySystem.HasOperator(h.Id))
            return false;
        if (h is not IRobot)
            return false;
        if (BuffSystem.TryGetBuff(healthed, Buffs.RedCard, out Buff _))
            return false;

        OnRevive(h);
        return true;
    }

    protected virtual void OnRevive(IHealthed healthed)
    {
        Logger.Info($"Revive {healthed.Id}");
        Publisher.PublishAsync(
            new ReviveEvent { Time = TimeSystem.Time, Reviver = healthed.Id }
        );
    }

    /// <summary>
    ///     减少生命值
    /// </summary>
    /// <param name="healthed"></param>
    /// <param name="operatorId"></param>
    /// <param name="value"></param>
    /// <returns>实际减少的生命值</returns>
    public uint DecreaseHealth(IHealthed healthed, in Identity operatorId, uint value)
    {
        var currentHealth = healthed.Health;
        if (currentHealth < value) value = currentHealth;

        if (currentHealth <= 0) return 0;

        var newHealth = currentHealth - value;

        SetHealth(healthed, newHealth);
        if (healthed.Health == 0) OnKill(healthed, operatorId);

        return value;
    }

    /// <summary>
    ///     增加生命值
    /// </summary>
    /// <param name="healthed"></param>
    /// <param name="value"></param>
    /// <returns>实际增加的生命值</returns>
    public virtual uint IncreaseHealth(IHealthed healthed, uint value)
    {
        var currentHealth = healthed.Health;
        var newHealth = currentHealth + value;
        if (newHealth > PerformanceSystem.GetMaxHealth(healthed))
        {
            value = PerformanceSystem.GetMaxHealth(healthed) - currentHealth;
            newHealth = PerformanceSystem.GetMaxHealth(healthed);
        }

        SetHealth(healthed, newHealth);

        return value;
    }

    protected void SetHealth(IHealthed healthed, uint value)
    {
        if (BuffSystem.TryGetBuff(healthed.Id, Buffs.RedCard, out Buff _))
            // 红牌状态下不允许设置生命值
            value = 0;

        value = Math.Clamp(value, 0, PerformanceSystem.GetMaxHealth(healthed));

        UIntCacheBox.WithWriterNamespace(healthed.Id).Save(IHealthed.HealthCacheKey, value);
    }

    public void ResetHealth(IHealthed healthed)
    {
        SetHealth(healthed, PerformanceSystem.GetMaxHealth(healthed));
    }

    public void SetInvincible(IHealthed healthed, bool value)
    {
        SetInvincible(healthed.Id, value);
    }

    public void SetInvincible(in Identity healthed, bool value, int seconds = 0)
    {
        if (value)
        {
            var duration = TimeSpan.MaxValue;
            if (seconds > 0) duration = TimeSpan.FromSeconds(seconds);
            BuffSystem.AddBuff(healthed, Buffs.DefenceBuff, 1, duration);
        }
        else
        {
            if (
                BuffSystem.TryGetBuff(healthed, Buffs.DefenceBuff, out Buff buff)
                && buff.IsInfinite
            )
                BuffSystem.RemoveBuff(healthed, Buffs.DefenceBuff);
        }
    }

    public bool IsInvincible(IHealthed healthed)
    {
        return BuffSystem.TryGetBuff(healthed.Id, Buffs.DefenceBuff, out float buff)
               && Math.Abs(buff - 1) < 0.01f;
    }
}