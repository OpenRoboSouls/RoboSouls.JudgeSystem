using System.Threading;
using System.Threading.Tasks;
using RoboSouls.JudgeSystem.Entities;
using RoboSouls.JudgeSystem.Systems;
using VContainer;

namespace RoboSouls.JudgeSystem.RoboMaster2024UL.Systems;

/// <summary>
/// 补给系统
/// </summary>
public sealed class SupplySystem : ISystem
{
    public const ushort SupplyZoneId = 50;
    public static readonly Identity RedSupplyZoneId = new Identity(Camp.Red, SupplyZoneId);
    public static readonly Identity BlueSupplyZoneId = new Identity(Camp.Blue, SupplyZoneId);
    private uint _blueSentryReviveAmountConsumed = 0;

    // did not use cache provider because client does not need to know the value
    private uint _redSentryReviveAmountConsumed = 0;

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
    internal PerformanceSystemBase aPerformanceSystemBase { get; set; }

    [Inject]
    internal LifeSystem LifeSystem { get; set; }

    [Inject]
    internal ModuleSystemBase ModuleSystem { get; set; }

    public Task Reset(CancellationToken cancellation = new CancellationToken())
    {
        TimeSystem.RegisterRepeatAction(1, SupplyUpdateLoop);

        _redSentryReviveAmountConsumed = 0;
        _blueSentryReviveAmountConsumed = 0;

        return Task.CompletedTask;
    }

    public bool IAmmoSupplyAllowed(in Identity entity)
    {
        if (TimeSystem.Stage is not JudgeSystemStage.Match)
            return false;

        if (
            !EntitySystem.TryGetEntity(entity, out IShooter s)
            || !EntitySystem.HasOperator(entity)
        )
            return false;

        if (entity.Camp == Camp.Red)
        {
            return ZoneSystem.IsInZone(entity, RedSupplyZoneId);
        }

        if (entity.Camp == Camp.Blue)
        {
            return ZoneSystem.IsInZone(entity, BlueSupplyZoneId);
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

    private Task SupplyUpdateLoop()
    {
        return Task.WhenAll(
            SupplyUpdateLoopFor(Identity.BlueHero),
            SupplyUpdateLoopFor(Identity.RedHero),
            SupplyUpdateLoopFor(Identity.BlueInfantry1),
            SupplyUpdateLoopFor(Identity.BlueInfantry2),
            SupplyUpdateLoopFor(Identity.RedInfantry1),
            SupplyUpdateLoopFor(Identity.RedInfantry2),
            SupplyUpdateLoopFor(Identity.BlueSentry),
            SupplyUpdateLoopFor(Identity.RedSentry)
        );
    }

    /// <summary>
    /// 英雄、步兵机器人： 占领己方补给区时，可以以每秒 10%上限血量的速度回血。若机器人处于脱战状态，
    /// 该数值将提升至 25%。
    /// 哨兵机器人： 比赛开始后至第 4 分钟（即倒计时 4:59-1:00），占领己方补给区后，将以每秒 100 点血量
    ///     的速度回血。以此种方式累计恢复的血量最高为 600。
    /// </summary>
    /// <returns></returns>
    private Task SupplyUpdateLoopFor(in Identity id)
    {
        if (TimeSystem.Stage != JudgeSystemStage.Match)
            return Task.CompletedTask;
        if (!EntitySystem.HasOperator(id))
            return Task.CompletedTask;
        if (!EntitySystem.TryGetEntity(id, out IHealthed h))
            return Task.CompletedTask;
        if (!ZoneSystem.IsInZone(id, id.Camp == Camp.Red ? RedSupplyZoneId : BlueSupplyZoneId))
            return Task.CompletedTask;

        ModuleSystem.SetGunLocked(id, false);

        if (id.IsHero() || id.IsInfantry())
        {
            var rate = BuffSystem.TryGetBuff(id, Buffs.OutOfCombatBuff, out Buff _)
                ? 0.25f
                : 0.1f;
            var reviveAmount = (uint)(aPerformanceSystemBase.GetMaxHealth(h) * rate);
            LifeSystem.IncreaseHealth(h, reviveAmount);
        }
        else if (id.IsSentry())
        {
            if (TimeSystem.StageTimeElapsed > 300)
                return Task.CompletedTask;
            if (id.Camp == Camp.Red)
            {
                if (_redSentryReviveAmountConsumed >= 600)
                    return Task.CompletedTask;
                _redSentryReviveAmountConsumed += LifeSystem.IncreaseHealth(h, 100);
            }
            else if (id.Camp == Camp.Blue)
            {
                if (_blueSentryReviveAmountConsumed >= 600)
                    return Task.CompletedTask;
                _blueSentryReviveAmountConsumed += LifeSystem.IncreaseHealth(h, 100);
            }
        }

        return Task.CompletedTask;
    }
}