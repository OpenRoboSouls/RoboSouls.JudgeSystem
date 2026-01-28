using System;
using RoboSouls.JudgeSystem.Entities;
using RoboSouls.JudgeSystem.RoboMaster2026UL.Entities;
using RoboSouls.JudgeSystem.Systems;

namespace RoboSouls.JudgeSystem.RoboMaster2026UL;

public sealed class RM2026ulPerformanceSystem : PerformanceSystemBase
{
    public const int InitialWinPoint = 200;

    public const int MaxPowerFallback = 70;
    public const int MaxHealthFallback = 200;


    private const int HeroMaxBulletSpeed = 16;
    private const int InfantryMaxBulletSpeed = 25;

    private const int SentryMaxPower = 100;
    private const int SentryMaxHealth = 400;
    private const int SentryMaxHeat = 260;
    private const int SentryCooldown = 30;

    public const int HeroMaxPower = 100;
    public const int HeroMaxHealth = 350;
    public const int HeroMaxHeat = 200;
    public const int HeroCooldown = 24;

    // 功率优先
    public const int InfantryMaxPowerPowerPriority = 90;

    public const int InfantryMaxHealthPowerPriority = 200;

    // 血量优先
    public const int InfantryMaxPowerHealthPriority = 75;

    public const int InfantryMaxHealthHealthPriority = 350;

    // 爆发优先
    public const int InfantryMaxHeatBurst = 230;

    public const int InfantryCooldownBurst = 14;

    // 冷却优先
    public const int InfantryMaxHeatCooldown = 88;
    public const int InfantryCooldownCooldown = 24;

    public override int GetStageTimeLimit(JudgeSystemStage stage)
    {
        return stage switch
        {
            JudgeSystemStage.OutOfMatch => int.MaxValue,
            JudgeSystemStage.Repair => 40,
            JudgeSystemStage.SelfCheck => 10,
            JudgeSystemStage.Countdown => 5,
            JudgeSystemStage.Match => 5 * 60,
            JudgeSystemStage.Settlement => 10,
            _ => throw new ArgumentOutOfRangeException(nameof(stage), stage, null)
        };
    }

    public override int GetLevel(IExperienced experienced)
    {
        return -1;
    }

    public override int GetLevelExpLength(IExperienced experienced)
    {
        return -1;
    }

    public override int GetLevelExpGained(IExperienced experienced)
    {
        return -1;
    }

    public override int GetMaxPower(IChassisd chassisd)
    {
        if (chassisd is Sentry)
            return SentryMaxPower;

        if (chassisd is Hero)
            return HeroMaxPower;

        if (chassisd is not Infantry)
            return MaxPowerFallback;

        return chassisd.ChassisType switch
        {
            ChassisTypePower => InfantryMaxPowerPowerPriority,
            ChassisTypeHealth => InfantryMaxPowerHealthPriority,
            _ => MaxPowerFallback
        };
    }

    public override int GetBasePower(IChassisd chassisd)
    {
        if (chassisd is Sentry)
            return SentryMaxPower;

        if (chassisd is Hero)
            return HeroMaxPower;

        if (chassisd is not Infantry)
            return MaxPowerFallback;

        return chassisd.ChassisType switch
        {
            ChassisTypePower => InfantryMaxPowerPowerPriority,
            ChassisTypeHealth => InfantryMaxPowerHealthPriority,
            _ => MaxPowerFallback
        };
    }

    public override uint GetMaxHealth(IHealthed healthed, int level = -1)
    {
        if (level == -1 && healthed is IExperienced exp) level = GetLevel(exp);

        if (healthed is Sentry)
            return SentryMaxHealth;

        if (healthed is Hero)
            return HeroMaxHealth;

        if (healthed is not Infantry inf)
            return MaxHealthFallback;

        return inf.ChassisType switch
        {
            ChassisTypePower => InfantryMaxHealthPowerPriority,
            ChassisTypeHealth => InfantryMaxHealthHealthPriority,
            _ => (uint)MaxHealthFallback
        };
    }

    public override float GetMaxHeat(IShooter shooter)
    {
        if (shooter is Sentry)
            return SentryMaxHeat;
        if (shooter is Hero)
            return HeroMaxHeat;
        if (shooter is not Infantry inf)
            return 0;

        return inf.GunType switch
        {
            GunType17mmBurst => InfantryMaxHeatBurst,
            GunType42mmDefault => InfantryMaxHeatCooldown,
            _ => 0
        };
    }

    public override int GetHeatDelta(IShooter shooter)
    {
        return shooter.AmmoType switch
        {
            AmmoType42mm => 100,
            AmmoType17mm => 10,
            _ => 0
        };
    }

    public override int GetCooldown(IShooter shooter)
    {
        if (shooter is Sentry)
            return SentryCooldown;
        if (shooter is Hero)
            return HeroCooldown;
        if (shooter is not Infantry inf)
            return 0;

        return inf.GunType switch
        {
            GunType17mmBurst => InfantryCooldownBurst,
            GunType42mmDefault => InfantryCooldownCooldown,
            _ => 0
        };
    }

    public override int GetMaxBulletSpeed(in Identity shooter)
    {
        if (shooter.IsHero())
            return HeroMaxBulletSpeed;
        if (shooter.IsInfantry() || shooter.IsSentry())
            return InfantryMaxBulletSpeed;
        return 0;
    }

    public static int GetLevel(int exp)
    {
        return -1;
    }
}