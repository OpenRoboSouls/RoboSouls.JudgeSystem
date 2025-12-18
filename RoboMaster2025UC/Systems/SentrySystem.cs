using System;
using System.Threading;
using System.Threading.Tasks;
using RoboSouls.JudgeSystem.Events;
using RoboSouls.JudgeSystem.RoboMaster2025UC.Entities;
using RoboSouls.JudgeSystem.Systems;
using VContainer;
using VitalRouter;

namespace RoboSouls.JudgeSystem.RoboMaster2025UC.Systems;

/// <summary>
/// 哨兵机器人机制
/// </summary>
[Routes]
public sealed partial class SentrySystem : ISystem
{
    [Inject]
    internal void Inject(Router router)
    {
        MapTo(router);
    }

    private static readonly int FreeAmmoAmountCacheKey = "free_ammo_amount".Sum();

    [Inject]
    internal ITimeSystem TimeSystem { get; set; }

    [Inject]
    internal BattleSystem BattleSystem { get; set; }

    [Inject]
    internal EntitySystem EntitySystem { get; set; }

    [Inject]
    internal ICacheProvider<int> IntCacheBox { get; set; }

    [Inject]
    internal LifeSystem LifeSystem { get; set; }

    public Task Reset(CancellationToken cancellation = new CancellationToken())
    {
        TimeSystem.RegisterOnceAction(
            JudgeSystemStage.Match,
            0,
            () =>
            {
                AddSentryAmmo(Camp.Red, 300);
                AddSentryAmmo(Camp.Blue, 300);
            }
        );

        var bothAddFreeAmmo = new Action(() =>
        {
            SetFreeAmmoAmount(Camp.Red, GetFreeAmmoAmount(Camp.Red) + 100);
            SetFreeAmmoAmount(Camp.Blue, GetFreeAmmoAmount(Camp.Blue) + 100);
        });

        TimeSystem.RegisterOnceAction(JudgeSystemStage.Match, 60, bothAddFreeAmmo);
        TimeSystem.RegisterOnceAction(JudgeSystemStage.Match, 120, bothAddFreeAmmo);
        TimeSystem.RegisterOnceAction(JudgeSystemStage.Match, 180, bothAddFreeAmmo);
        TimeSystem.RegisterOnceAction(JudgeSystemStage.Match, 240, bothAddFreeAmmo);
        TimeSystem.RegisterOnceAction(JudgeSystemStage.Match, 300, bothAddFreeAmmo);
        TimeSystem.RegisterOnceAction(JudgeSystemStage.Match, 360, bothAddFreeAmmo);

        return Task.CompletedTask;
    }

    private void AddSentryAmmo(Camp camp, int amount)
    {
        var id = camp switch
        {
            Camp.Red => Identity.RedSentry,
            Camp.Blue => Identity.BlueSentry,
            _ => throw new ArgumentOutOfRangeException(nameof(camp), camp, null),
        };

        if (!EntitySystem.TryGetOperatedEntity(id, out Sentry s))
            return;

        BattleSystem.SetAmmoAllowance(s, s.AmmoAllowance + amount);
    }

    /// <summary>
    /// 占领补给区自动获取的允许发弹量
    /// </summary>
    /// <param name="camp"></param>
    /// <returns></returns>
    public int GetFreeAmmoAmount(Camp camp)
    {
        var id = camp switch
        {
            Camp.Red => Identity.RedSentry,
            Camp.Blue => Identity.BlueSentry,
            _ => throw new ArgumentOutOfRangeException(nameof(camp), camp, null),
        };

        if (!EntitySystem.TryGetOperatedEntity(id, out Sentry s))
            return 0;

        return IntCacheBox.WithReaderNamespace(id).Load(FreeAmmoAmountCacheKey);
    }

    private void SetFreeAmmoAmount(Camp camp, int amount)
    {
        var id = camp switch
        {
            Camp.Red => Identity.RedSentry,
            Camp.Blue => Identity.BlueSentry,
            _ => throw new ArgumentOutOfRangeException(nameof(camp), camp, null),
        };

        if (!EntitySystem.TryGetOperatedEntity(id, out Sentry s))
            return;

        IntCacheBox.WithWriterNamespace(id).Save(FreeAmmoAmountCacheKey, amount);
    }

    [Route]
    private void OnEnterSupplyZone(EnterZoneEvent evt)
    {
        if (TimeSystem.Stage != JudgeSystemStage.Match)
            return;
        if (!evt.OperatorId.IsSentry())
            return;
        if (
            !(
                evt.ZoneId == SupplySystem.RedSupplyZoneId
                || evt.ZoneId == SupplySystem.BlueSupplyZoneId
            )
        )
            return;
        if (evt.OperatorId.Camp != evt.ZoneId.Camp)
            return;
        if (!EntitySystem.TryGetOperatedEntity(evt.OperatorId, out Sentry s))
            return;

        var amount = GetFreeAmmoAmount(evt.OperatorId.Camp);
        if (amount <= 0)
            return;

        BattleSystem.SetAmmoAllowance(s, s.AmmoAllowance + amount);
        SetFreeAmmoAmount(evt.OperatorId.Camp, 0);
        LifeSystem.SetInvincible(evt.OperatorId, false);
    }

    [Route]
    private void OnRevive(ReviveEvent evt)
    {
        if (TimeSystem.Stage != JudgeSystemStage.Match)
            return;
        if (!evt.Reviver.IsSentry())
            return;
        LifeSystem.SetInvincible(evt.Reviver, true, 60);
    }
}