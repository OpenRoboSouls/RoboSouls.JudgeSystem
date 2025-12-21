using System;
using System.Threading;
using System.Threading.Tasks;
using RoboSouls.JudgeSystem.Events;
using RoboSouls.JudgeSystem.RoboMaster2026UC.Entities;
using RoboSouls.JudgeSystem.Systems;
using VContainer;
using VitalRouter;

namespace RoboSouls.JudgeSystem.RoboMaster2026UC.Systems;

/// <summary>
/// 哨兵机器人机制
/// </summary>
[Routes]
public sealed partial class SentrySystem(
    ITimeSystem timeSystem,
    BattleSystem battleSystem,
    EntitySystem entitySystem,
    ICacheProvider<int> intCacheBox,
    LifeSystem lifeSystem)
    : ISystem
{
    [Inject]
    internal void Inject(Router router)
    {
        MapTo(router);
    }

    private static readonly int FreeAmmoAmountCacheKey = "free_ammo_amount".Sum();

    public Task Reset(CancellationToken cancellation = new CancellationToken())
    {
        timeSystem.RegisterOnceAction(
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

        timeSystem.RegisterOnceAction(JudgeSystemStage.Match, 60, bothAddFreeAmmo);
        timeSystem.RegisterOnceAction(JudgeSystemStage.Match, 120, bothAddFreeAmmo);
        timeSystem.RegisterOnceAction(JudgeSystemStage.Match, 180, bothAddFreeAmmo);
        timeSystem.RegisterOnceAction(JudgeSystemStage.Match, 240, bothAddFreeAmmo);
        timeSystem.RegisterOnceAction(JudgeSystemStage.Match, 300, bothAddFreeAmmo);
        timeSystem.RegisterOnceAction(JudgeSystemStage.Match, 360, bothAddFreeAmmo);

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

        if (!entitySystem.TryGetOperatedEntity(id, out Sentry s))
            return;

        battleSystem.SetAmmoAllowance(s, s.AmmoAllowance + amount);
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

        if (!entitySystem.TryGetOperatedEntity(id, out Sentry s))
            return 0;

        return intCacheBox.WithReaderNamespace(id).Load(FreeAmmoAmountCacheKey);
    }

    private void SetFreeAmmoAmount(Camp camp, int amount)
    {
        var id = camp switch
        {
            Camp.Red => Identity.RedSentry,
            Camp.Blue => Identity.BlueSentry,
            _ => throw new ArgumentOutOfRangeException(nameof(camp), camp, null),
        };

        if (!entitySystem.TryGetOperatedEntity(id, out Sentry s))
            return;

        intCacheBox.WithWriterNamespace(id).Save(FreeAmmoAmountCacheKey, amount);
    }

    [Route]
    private void OnEnterSupplyZone(EnterZoneEvent evt)
    {
        if (timeSystem.Stage != JudgeSystemStage.Match)
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
        if (!entitySystem.TryGetOperatedEntity(evt.OperatorId, out Sentry s))
            return;

        var amount = GetFreeAmmoAmount(evt.OperatorId.Camp);
        if (amount <= 0)
            return;

        battleSystem.SetAmmoAllowance(s, s.AmmoAllowance + amount);
        SetFreeAmmoAmount(evt.OperatorId.Camp, 0);
        lifeSystem.SetInvincible(evt.OperatorId, false);
    }

    [Route]
    private void OnRevive(ReviveEvent evt)
    {
        if (timeSystem.Stage != JudgeSystemStage.Match)
            return;
        if (!evt.Reviver.IsSentry())
            return;
        lifeSystem.SetInvincible(evt.Reviver, true, 60);
    }
}