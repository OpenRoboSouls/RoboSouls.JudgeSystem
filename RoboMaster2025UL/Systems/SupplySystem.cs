using System.Threading;
using System.Threading.Tasks;
using RoboSouls.JudgeSystem.Entities;
using RoboSouls.JudgeSystem.Systems;
using VContainer;

namespace RoboSouls.JudgeSystem.RoboMaster2025UL.Systems;

/// <summary>
/// 补给系统
/// </summary>
public sealed class SupplySystem : ISystem
{
    public const ushort SupplyZoneId = 50;
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
    internal PerformanceSystemBase aPerformanceSystemBase { get; set; }

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

    private static int GetPerAmmoPrice(byte ammoType)
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

        var deposit = camp == Camp.Red ? EconomySystem.RedCoin : EconomySystem.BlueCoin;
        var price = GetPerAmmoPrice(ammoType);
        return deposit / price;
    }

    public bool CheckCanBuy(in Identity user, byte ammoType, int amount, out int cost)
    {
        if (user.IsSentry())
        {
            cost = 0;
            return false;
        }

        cost = GetPerAmmoPrice(ammoType) * amount;
        return GetTeamAmmoAllowance(user.Camp, ammoType) >= amount;
    }

    public void BuyAmmo(IShooter shooter, int amount)
    {
        if (!CheckCanBuy(shooter.Id, shooter.AmmoType, amount, out var cost))
            return;

        if (shooter.Id.Camp == Camp.Red)
        {
            EconomySystem.RedCoin -= cost;
        }
        else if (shooter.Id.Camp == Camp.Blue)
        {
            EconomySystem.BlueCoin -= cost;
        }

        BattleSystem.SetAmmoAllowance(shooter, shooter.AmmoAllowance + amount);
    }

    private Task SupplyUpdateLoop()
    {
        return Task.WhenAll(
            SupplyUpdateLoopFor(Identity.BlueHero),
            SupplyUpdateLoopFor(Identity.RedHero),
            SupplyUpdateLoopFor(Identity.BlueInfantry1),
            SupplyUpdateLoopFor(Identity.RedInfantry1),
            SupplyUpdateLoopFor(Identity.BlueSentry),
            SupplyUpdateLoopFor(Identity.RedSentry)
        );
    }

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

        var reviveAmount = (uint)(aPerformanceSystemBase.GetMaxHealth(h) * 0.25f);
        LifeSystem.IncreaseHealth(h, reviveAmount);

        return Task.CompletedTask;
    }
}