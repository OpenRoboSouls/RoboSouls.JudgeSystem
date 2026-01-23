using System;
using System.Threading;
using System.Threading.Tasks;
using RoboSouls.JudgeSystem.Entities;
using RoboSouls.JudgeSystem.Systems;

namespace RoboSouls.JudgeSystem.RoboMaster2026UC.Systems;

/// <summary>
/// 补给系统
/// </summary>
public sealed class SupplySystem(
    EntitySystem entitySystem,
    ZoneSystem zoneSystem,
    ITimeSystem timeSystem,
    EconomySystem economySystem,
    BattleSystem battleSystem,
    BuffSystem buffSystem,
    PerformanceSystemBase performanceSystem,
    LifeSystem lifeSystem,
    ModuleSystemBase moduleSystem)
    : ISystem
{
    public const ushort SupplyZoneId = 70;
    public static readonly Identity RedSupplyZoneId = new Identity(Camp.Red, SupplyZoneId);
    public static readonly Identity BlueSupplyZoneId = new Identity(Camp.Blue, SupplyZoneId);

    public Task Reset(CancellationToken cancellation = new CancellationToken())
    {
        timeSystem.RegisterRepeatAction(1, SupplyUpdateLoop);

        return Task.CompletedTask;
    }

    public bool IsAmmoSupplyAllowed(in Identity entity)
    {
        if (timeSystem.Stage is not JudgeSystemStage.Match)
            return false;

        if (
            !entitySystem.TryGetEntity(entity, out IShooter s)
            || !entitySystem.HasOperator(entity)
        )
            return false;

        return IsInSupplyZone(zoneSystem, entity)
               || zoneSystem.IsInZone(
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
            return zoneSystem.IsInZone(entity, RedSupplyZoneId);
        }

        if (entity.Camp == Camp.Blue)
        {
            return zoneSystem.IsInZone(entity, BlueSupplyZoneId);
        }

        return false;
    }

    private int GetPerAmmoPrice(byte ammoType)
    {
        return ammoType switch
        {
            PerformanceSystemBase.AmmoType17mm => 1,
            PerformanceSystemBase.AmmoType42mm => 10,
            _ => int.MaxValue,
        };
    }

    public int GetTeamAmmoAllowance(Camp camp, byte ammoType)
    {
        if (camp is not (Camp.Blue or Camp.Red))
            return 0;

        var deposit = camp == Camp.Red ? economySystem.RedCoin : economySystem.BlueCoin;
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

        if (!economySystem.TryDecreaseCoin(shooter.Id.Camp, cost))
        {
            return;
        }

        battleSystem.SetAmmoAllowance(shooter, shooter.AmmoAllowance + amount);
    }

    public bool TryRemoteSupplyBlood(Identity id)
    {
        if (!entitySystem.TryGetEntity(id, out IHealthed healthed))
            return false;
        var cost = CalcRemoteSupplyBloodPrice();
        if (!economySystem.TryDecreaseCoin(id.Camp, cost))
        {
            return false;
        }

        timeSystem.RegisterOnceAction(
            6,
            () =>
            {
                lifeSystem.IncreaseHealth(
                    healthed,
                    (uint)(performanceSystem.GetMaxHealth(healthed) * 0.6f)
                );
            }
        );

        return true;
    }

    public bool TryRemoteSupplyAmmo17Mm(Identity id)
    {
        if (!entitySystem.TryGetEntity(id, out IShooter shooter))
            return false;
        if (shooter.AmmoType != PerformanceSystemBase.AmmoType17mm)
            return false;
        const int cost = 150;
        if (!economySystem.TryDecreaseCoin(id.Camp, cost))
        {
            return false;
        }

        timeSystem.RegisterOnceAction(
            6,
            () =>
            {
                battleSystem.SetAmmoAllowance(shooter, shooter.AmmoAllowance + 100);
            }
        );

        return true;
    }

    public bool TryRemoteSupplyAmmo42Mm(Identity id)
    {
        if (!entitySystem.TryGetEntity(id, out IShooter shooter))
            return false;
        if (shooter.AmmoType != PerformanceSystemBase.AmmoType42mm)
            return false;
        const int cost = 150;
        if (!economySystem.TryDecreaseCoin(id.Camp, cost))
        {
            return false;
        }

        timeSystem.RegisterOnceAction(
            6,
            () =>
            {
                battleSystem.SetAmmoAllowance(shooter, shooter.AmmoAllowance + 10);
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
        return (int)(Math.Round(timeSystem.StageTimeElapsed / 60 * 20) + 50);
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
            timeSystem.Stage != JudgeSystemStage.Match
            || !entitySystem.TryGetOperatedEntity(id, out IHealthed healthed)
            || !id.IsHero() && !id.IsInfantry() && !id.IsSentry() && !id.IsEngineer()
            || healthed.IsDead()
        )
            return Task.CompletedTask;

        if (
            zoneSystem.IsInZone(id, id.Camp == Camp.Red ? RedSupplyZoneId : BlueSupplyZoneId)
        )
        {
            moduleSystem.SetGunLocked(id, false);
            var rate = 0.1f;
            if (
                IsInSupplyZone(zoneSystem, id)
                && timeSystem.StageTimeElapsed > 240
                && buffSystem.TryGetBuff(id, Buffs.OutOfCombatBuff, out Buff _)
            )
            {
                rate = 0.25f;
            }

            var reviveAmount = (uint)
                Math.Ceiling(performanceSystem.GetMaxHealth(healthed) * rate);
            lifeSystem.IncreaseHealth(healthed, reviveAmount);
            buffSystem.AddBuff(id, Buffs.PowerBuff, 2f, TimeSpan.MaxValue);
                
            buffSystem.RemoveBuff(id, RM2026ucBuffs.WeakenedBuff);
        }
        else if (
            zoneSystem.IsInZone(
                id,
                id.Camp == Camp.Red
                    ? OutpostSystem.RedOutpostZoneId
                    : OutpostSystem.BlueOutpostZoneId
            )
        )
        {
            const float rate = 0.05f;
            var reviveAmount = (uint)
                Math.Ceiling(performanceSystem.GetMaxHealth(healthed) * rate);
            lifeSystem.IncreaseHealth(healthed, reviveAmount);
        }

        return Task.CompletedTask;
    }
}