using System;
using System.Collections.ObjectModel;
using RoboSouls.JudgeSystem.Entities;
using RoboSouls.JudgeSystem.RoboMaster2024UL.Entities;
using RoboSouls.JudgeSystem.Systems;

namespace RoboSouls.JudgeSystem.RoboMaster2024UL;

public sealed class RM2024ulPerformanceSystem : PerformanceSystemBase
{
    private const int SentryMaxPower = 100;

    public const int MaxPowerFallback = 70;

    private const int SentryMaxHealth = 400;
    private const int BaseMaxHealth = 1500;
    public const int MaxHealthFallback = 200;

    private const int SentryMaxHeat = 400;

    private const int SentryCooldown = 80;

    private const int HeroMaxBulletSpeed = 16;
    private const int InfantryMaxBulletSpeed = 25;

    private static readonly ReadOnlyCollection<int> ExperienceTree = Array.AsReadOnly(
        new[] { 400, 800, 1200, 1600, 2000, 2400, 2800, 3200, 4000 }
    );

    /// <summary>
    /// 功率优先
    /// </summary>
    private static readonly ReadOnlyCollection<int> PowerTreeHeroPowerPriority =
        Array.AsReadOnly(new[] { 70, 75, 80, 85, 90, 95, 100, 105, 110, 120 });

    /// <summary>
    /// 血量优先
    /// </summary>
    private static readonly ReadOnlyCollection<int> PowerTreeHeroHealthPriority =
        Array.AsReadOnly(new[] { 55, 60, 65, 70, 75, 80, 85, 90, 100, 120 });

    /// <summary>
    /// 功率优先
    /// </summary>
    private static readonly ReadOnlyCollection<int> PowerTreeInfantryPowerPriority =
        Array.AsReadOnly(new[] { 60, 65, 70, 75, 80, 85, 90, 95, 100, 100 });

    /// <summary>
    /// 血量优先
    /// </summary>
    private static readonly ReadOnlyCollection<int> PowerTreeInfantryHealthPriority =
        Array.AsReadOnly(new[] { 45, 50, 55, 60, 65, 70, 75, 80, 90, 100 });

    /// <summary>
    /// 功率优先
    /// </summary>
    private static readonly ReadOnlyCollection<uint> HealthTreeHeroPowerPriority =
        Array.AsReadOnly(new uint[] { 200, 225, 250, 275, 300, 325, 350, 375, 400, 500 });

    /// <summary>
    /// 血量优先
    /// </summary>
    private static readonly ReadOnlyCollection<uint> HealthTreeHeroHealthPriority =
        Array.AsReadOnly(new uint[] { 250, 275, 300, 325, 350, 375, 400, 425, 450, 500 });

    /// <summary>
    /// 功率优先
    /// </summary>
    private static readonly ReadOnlyCollection<uint> HealthTreeInfantryPowerPriority =
        Array.AsReadOnly(new uint[] { 150, 175, 200, 225, 250, 275, 300, 325, 350, 400 });

    /// <summary>
    /// 血量优先
    /// </summary>
    private static readonly ReadOnlyCollection<uint> HealthTreeInfantryHealthPriority =
        Array.AsReadOnly(new uint[] { 200, 225, 250, 275, 300, 325, 350, 375, 400, 400 });

    /// <summary>
    /// 默认
    /// </summary>
    private static readonly ReadOnlyCollection<float> HeatTree42mmDefault = Array.AsReadOnly(
        new float[] { 100, 140, 180, 220, 260, 300, 340, 380, 420, 500 }
    );

    /// <summary>
    /// 爆发优先
    /// </summary>
    private static readonly ReadOnlyCollection<float> HeatTree17mmBurst = Array.AsReadOnly(
        new float[] { 200, 250, 300, 350, 400, 450, 500, 550, 600, 650 }
    );

    /// <summary>
    /// 冷却优先
    /// </summary>
    private static readonly ReadOnlyCollection<float> HeatTree17mmCooldown = Array.AsReadOnly(
        new float[] { 50, 85, 120, 155, 190, 225, 260, 295, 330, 400 }
    );

    /// <summary>
    /// 默认
    /// </summary>
    private static readonly ReadOnlyCollection<int> CooldownTree42mmDefault = Array.AsReadOnly(
        new int[] { 40, 48, 56, 64, 72, 80, 88, 96, 104, 120 }
    );

    /// <summary>
    /// 爆发优先
    /// </summary>
    private static readonly ReadOnlyCollection<int> CooldownTree17mmBurst = Array.AsReadOnly(
        new int[] { 40, 45, 50, 55, 60, 65, 70, 75, 80, 80 }
    );

    /// <summary>
    /// 冷却优先
    /// </summary>
    private static readonly ReadOnlyCollection<int> CooldownTree17mmCooldown = Array.AsReadOnly(
        new int[] { 50, 85, 120, 155, 190, 225, 260, 295, 330, 400 }
    );

    public static uint GetBaseMaxShield => 1500;

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
            _ => throw new ArgumentOutOfRangeException(nameof(stage), stage, null),
        };
    }

    public override int GetLevel(IExperienced experienced)
    {
        var exp = experienced.Experience;
        return GetLevel(exp);
    }

    public override int GetLevelExpLength(IExperienced experienced)
    {
        var level = GetLevel(experienced);
        level = Math.Clamp(level, 1, ExperienceTree.Count);
        return ExperienceTree[level - 1];
    }

    public override int GetLevelExpGained(IExperienced experienced)
    {
        var level = GetLevel(experienced);
        var lastLevelExp = level == 1 ? 0 : ExperienceTree[level - 2];
        return experienced.Experience - lastLevelExp;
    }

    public override int GetMaxPower(IChassisd chassisd)
    {
        return chassisd switch
        {
            Hero h => chassisd.ChassisType switch
            {
                ChassisTypePower => PowerTreeHeroPowerPriority[GetLevel(h) - 1],
                ChassisTypeHealth => PowerTreeHeroHealthPriority[GetLevel(h) - 1],
                _ => MaxPowerFallback,
            },
            Infantry i => chassisd.ChassisType switch
            {
                ChassisTypePower => PowerTreeInfantryPowerPriority[GetLevel(i) - 1],
                ChassisTypeHealth => PowerTreeInfantryHealthPriority[GetLevel(i) - 1],
                _ => MaxPowerFallback,
            },
            Sentry => SentryMaxPower,
            _ => MaxPowerFallback,
        };
    }

    public override int GetBasePower(IChassisd chassisd)
    {
        return chassisd switch
        {
            Hero h => h.ChassisType switch
            {
                ChassisTypePower => PowerTreeHeroPowerPriority[0],
                ChassisTypeHealth => PowerTreeHeroHealthPriority[0],
                _ => MaxPowerFallback,
            },
            Infantry i => i.ChassisType switch
            {
                ChassisTypePower => PowerTreeInfantryPowerPriority[0],
                ChassisTypeHealth => PowerTreeInfantryHealthPriority[0],
                _ => MaxPowerFallback,
            },
            Sentry => SentryMaxPower,
            _ => MaxPowerFallback,
        };
    }

    public override uint GetMaxHealth(IHealthed healthed, int level = -1)
    {
        if (level == -1 && healthed is IExperienced exp)
        {
            level = GetLevel(exp);
        }

        return healthed switch
        {
            Hero h => h.ChassisType switch
            {
                ChassisTypePower => HealthTreeHeroPowerPriority[level - 1],
                ChassisTypeHealth => HealthTreeHeroHealthPriority[level - 1],
                _ => MaxHealthFallback,
            },
            Infantry i => i.ChassisType switch
            {
                ChassisTypePower => HealthTreeInfantryPowerPriority[level - 1],
                ChassisTypeHealth => HealthTreeInfantryHealthPriority[level - 1],
                _ => MaxHealthFallback,
            },
            Sentry => SentryMaxHealth,
            Base => BaseMaxHealth,
            _ => MaxHealthFallback,
        };
    }

    public override float GetMaxHeat(IShooter shooter)
    {
        if (shooter is Sentry)
            return SentryMaxHeat;
        if (shooter is not IExperienced exp)
            return 0;
        var level = GetLevel(exp);

        return shooter.GunType switch
        {
            GunType17mmBurst => HeatTree17mmBurst[level - 1],
            GunType17mmCooldown => HeatTree17mmCooldown[level - 1],
            GunType42mmDefault => HeatTree42mmDefault[level - 1],
            _ => 0,
        };
    }

    public override int GetHeatDelta(IShooter shooter)
    {
        return shooter.AmmoType switch
        {
            AmmoType42mm => 100,
            AmmoType17mm => 10,
            _ => 0,
        };
    }

    public override int GetCooldown(IShooter shooter)
    {
        if (shooter is Sentry)
            return SentryCooldown;
        if (shooter is not IExperienced exp)
            return 0;
        var level = GetLevel(exp);

        return shooter.GunType switch
        {
            GunType17mmBurst => CooldownTree17mmBurst[level - 1],
            GunType17mmCooldown => CooldownTree17mmCooldown[level - 1],
            GunType42mmDefault => CooldownTree42mmDefault[level - 1],
            _ => 0,
        };
    }

    public override int GetMaxBulletSpeed(in Identity shooter)
    {
        // return shooter switch
        // {
        //     Hero => HeroMaxBulletSpeed,
        //     Infantry => InfantryMaxBulletSpeed,
        //     _ => 0
        // };
        if (shooter.IsHero())
            return HeroMaxBulletSpeed;
        if (shooter.IsInfantry())
            return InfantryMaxBulletSpeed;
        return 0;
    }

    public static int GetLevel(int exp)
    {
        for (var i = 0; i < ExperienceTree.Count; i++)
        {
            if (exp < ExperienceTree[i])
            {
                return i + 1;
            }
        }

        return ExperienceTree.Count + 1;
    }
}