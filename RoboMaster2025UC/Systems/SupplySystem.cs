using System;
using System.Threading;
using System.Threading.Tasks;
using RoboSouls.JudgeSystem.Entities;
using RoboSouls.JudgeSystem.Systems;
using VContainer;

namespace RoboSouls.JudgeSystem.RoboMaster2025UC.Systems;

/// <summary>
/// 补给系统
/// </summary>
public sealed class SupplySystem : ISystem
{
    public const ushort SupplyZoneId = 70;
    public static readonly Identity RedSupplyZoneId = new Identity(Camp.Red, SupplyZoneId);
    public static readonly Identity BlueSupplyZoneId = new Identity(Camp.Blue, SupplyZoneId);

    [Inject]
    internal ILogger Logger { get; set; }

    [Inject]
    internal ICacheProvider<uint> UintCacheBox { get; set; }

    [Inject]
    internal EntitySystem EntitySystem { get; set; }

    [Inject]
    internal ZoneSystem ZoneSystem { get; set; }

    [Inject]
    internal ITimeSystem TimeSystem { get; set; }

    [Inject]
    internal EconomySystem EconomySystem { get; set; }

    [Inject]
    internal BattleSystem BattleSystem { get; set; }

    [Inject]
    internal BuffSystem BuffSystem { get; set; }

    [Inject]
    internal PerformanceSystemBase PerformanceSystem { get; set; }

    [Inject]
    internal LifeSystem LifeSystem { get; set; }

    [Inject]
    internal ModuleSystemBase ModuleSystem { get; set; }

    public Task Reset(CancellationToken cancellation = new CancellationToken())
    {
        TimeSystem.RegisterRepeatAction(1, SupplyUpdateLoop);

        return Task.CompletedTask;
    }

    public bool IsAmmoSupplyAllowed(in Identity entity)
    {
        if (TimeSystem.Stage is not JudgeSystemStage.Match)
            return false;

        if (
            !EntitySystem.TryGetEntity(entity, out IShooter s)
            || !EntitySystem.HasOperator(entity)
        )
            return false;

        return IsInSupplyZone(ZoneSystem, entity)
               || ZoneSystem.IsInZone(
                   entity,
                   entity.Camp == Camp.Red
                       ? OutpostSystem.RedOutpostZoneId
                       : OutpostSystem.BlueOutpostZoneId
               );
    }

    public static bool IsInSupplyZone(ZoneSystem zoneSystem, in Identity entity)
    {
        if (entity.Camp == Camp.Red)
        {
            return zoneSystem.IsInZone(entity, RedSupplyZoneId)
                   || zoneSystem.IsInZone(entity, ExchangerSystem.RedExchangeZoneId);
        }

        if (entity.Camp == Camp.Blue)
        {
            return zoneSystem.IsInZone(entity, BlueSupplyZoneId)
                   || zoneSystem.IsInZone(entity, ExchangerSystem.BlueExchangeZoneId);
        }

        return false;
    }

    private int GetPerAmmoPrice(byte ammoType)
    {
        return ammoType switch
        {
            PerformanceSystemBase.AmmoType17mm => 1,
            PerformanceSystemBase.AmmoType42mm => 15,
            _ => int.MaxValue,
        };
    }

    public int GetTeamAmmoAllowance(Camp camp, byte ammoType)
    {
        if (camp is not (Camp.Blue or Camp.Red))
            return 0;

        var deposit = camp == Camp.Red ? EconomySystem.RedCoin : EconomySystem.BlueCoin;
        var price = GetPerAmmoPrice(ammoType);
        return deposit / price;
    }

    public bool CheckCanBuy(Camp camp, byte ammoType, int amount, out int cost)
    {
        cost = GetPerAmmoPrice(ammoType) * amount;
        return GetTeamAmmoAllowance(camp, ammoType) >= amount;
    }

    public void BuyAmmo(IShooter shooter, int amount)
    {
        if (!CheckCanBuy(shooter.Id.Camp, shooter.AmmoType, amount, out var cost))
            return;

        if (!EconomySystem.TryDecreaseCoin(shooter.Id.Camp, cost))
        {
            return;
        }

        BattleSystem.SetAmmoAllowance(shooter, shooter.AmmoAllowance + amount);
    }

    public bool TryRemoteSupplyBlood(Identity id)
    {
        if (!EntitySystem.TryGetEntity(id, out IHealthed healthed))
            return false;
        var cost = CalcRemoteSupplyBloodPrice();
        if (!EconomySystem.TryDecreaseCoin(id.Camp, cost))
        {
            return false;
        }

        TimeSystem.RegisterOnceAction(
            6,
            () =>
            {
                LifeSystem.IncreaseHealth(
                    healthed,
                    (uint)(PerformanceSystem.GetMaxHealth(healthed) * 0.6f)
                );
            }
        );

        return true;
    }

    public bool TryRemoteSupplyAmmo17mm(Identity id)
    {
        if (!EntitySystem.TryGetEntity(id, out IShooter shooter))
            return false;
        if (shooter.AmmoType != PerformanceSystemBase.AmmoType17mm)
            return false;
        const int cost = 150;
        if (!EconomySystem.TryDecreaseCoin(id.Camp, cost))
        {
            return false;
        }

        TimeSystem.RegisterOnceAction(
            6,
            () =>
            {
                BattleSystem.SetAmmoAllowance(shooter, shooter.AmmoAllowance + 100);
            }
        );

        return true;
    }

    public bool TryRemoteSupplyAmmo42mm(Identity id)
    {
        if (!EntitySystem.TryGetEntity(id, out IShooter shooter))
            return false;
        if (shooter.AmmoType != PerformanceSystemBase.AmmoType42mm)
            return false;
        const int cost = 200;
        if (!EconomySystem.TryDecreaseCoin(id.Camp, cost))
        {
            return false;
        }

        TimeSystem.RegisterOnceAction(
            6,
            () =>
            {
                BattleSystem.SetAmmoAllowance(shooter, shooter.AmmoAllowance + 10);
            }
        );

        return true;
    }

    public int CalcRemoteSupplyBloodPrice()
    {
        /*
         * 50 + ROUNDUP(420−比赛剩余时长
60
× 20) 金币/1 次
         */
        return (int)(Math.Round(TimeSystem.StageTimeElapsed / 60 * 20) + 50);
    }

    private Task SupplyUpdateLoop()
    {
        return Task.WhenAll(
            SupplyUpdateLoopFor(Identity.BlueHero),
            SupplyUpdateLoopFor(Identity.RedHero),
            SupplyUpdateLoopFor(Identity.BlueEngineer),
            SupplyUpdateLoopFor(Identity.RedEngineer),
            SupplyUpdateLoopFor(Identity.BlueInfantry1),
            SupplyUpdateLoopFor(Identity.BlueInfantry2),
            SupplyUpdateLoopFor(Identity.RedInfantry1),
            SupplyUpdateLoopFor(Identity.RedInfantry2),
            SupplyUpdateLoopFor(Identity.BlueSentry),
            SupplyUpdateLoopFor(Identity.RedSentry)
        );
    }

    /// <summary>
    /// 地面机器人占领己方补给区增益点时，将获得每秒上限血量 10%的回血增益。在比赛开始 4 分钟后，当机
    /// 器人处于脱战状态，且占领己方补给区增益点时，其将获得每秒上限血量 25%的回血增益，且底盘功率上
    ///     限提升 1 倍，但提升后的底盘功率上限为 200W。当机器人不处于脱战状态， 25%的回血增益和底盘功率
    ///     上限提升效果立即失效；当机器人未占领己方补给区增益点持续 4 秒后，底盘功率上限提升效果失效
    /// </summary>
    /// <returns></returns>
    private Task SupplyUpdateLoopFor(in Identity id)
    {
        if (
            TimeSystem.Stage != JudgeSystemStage.Match
            || !EntitySystem.TryGetOperatedEntity(id, out IHealthed healthed)
            || !id.IsHero() && !id.IsInfantry() && !id.IsSentry() && !id.IsEngineer()
            || healthed.IsDead()
        )
            return Task.CompletedTask;

        if (
            ZoneSystem.IsInZone(id, id.Camp == Camp.Red ? RedSupplyZoneId : BlueSupplyZoneId)
            || ZoneSystem.IsInZone(
                id,
                id.Camp == Camp.Red
                    ? ExchangerSystem.RedExchangeZoneId
                    : ExchangerSystem.BlueExchangeZoneId
            )
        )
        {
            ModuleSystem.SetGunLocked(id, false);
            var rate = 0.1f;
            if (
                IsInSupplyZone(ZoneSystem, id)
                && TimeSystem.StageTimeElapsed > 240
                && BuffSystem.TryGetBuff(id, Buffs.OutOfCombatBuff, out Buff _)
            )
            {
                rate = 0.25f;
            }

            var reviveAmount = (uint)
                Math.Ceiling(PerformanceSystem.GetMaxHealth(healthed) * rate);
            LifeSystem.IncreaseHealth(healthed, reviveAmount);
            BuffSystem.AddBuff(id, Buffs.PowerBuff, 2f, TimeSpan.MaxValue);
        }
        else if (
            ZoneSystem.IsInZone(
                id,
                id.Camp == Camp.Red
                    ? OutpostSystem.RedOutpostZoneId
                    : OutpostSystem.BlueOutpostZoneId
            )
        )
        {
            const float rate = 0.05f;
            var reviveAmount = (uint)
                Math.Ceiling(PerformanceSystem.GetMaxHealth(healthed) * rate);
            LifeSystem.IncreaseHealth(healthed, reviveAmount);
        }

        return Task.CompletedTask;
    }
}