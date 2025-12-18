using System;
using System.Collections.ObjectModel;
using RoboSouls.JudgeSystem.Entities;
using RoboSouls.JudgeSystem.RoboMaster2025UC.Entities;
using RoboSouls.JudgeSystem.RoboMaster2025UC.Systems;
using RoboSouls.JudgeSystem.Systems;

namespace RoboSouls.JudgeSystem.RoboMaster2025UC;

public sealed class RM2025ucPerformanceSystem : PerformanceSystemBase
{
    public const int SentryMaxPower = 100;
    public const int MaxPowerFallback = 70;

    public const int EngineerMaxHealth = 250;
    public const int SentryMaxHealth = 400;
    public const int OutpostMaxHealth = 1500;
    public const int BaseMaxHealth = 5000;
    public const int MaxHealthFallback = 200;

    public const int SentryMaxHeat = 400;

    public const int SentryCooldown = 80;

    public const int HeroMaxBulletSpeed = 16;
    public const int InfantryMaxBulletSpeed = 25;

    public const int MaxDartCount = 4;
    public const int MaxDoubleVulnerableChange = 2;

    public const int DartFlyDuration = 2;
    public const float DartFireInterval = 3;

    public const double DeploymentEnterTime = 3;

    public const double MaxAerialStrikeTime = 45;

    /// <summary>
    /// 经验体系
    ///
    /// 表 5-15 英雄、步兵机器人的等级和经验
    /// 等级 升级所需总经验
    /// 1 0
    /// 2 550
    /// 3 1100
    /// 4 1650
    /// 5 2200
    /// 6 2750
    /// 7 3300
    /// 8 3850
    /// 9 4400
    /// 10 5000
    /// </summary>
    private static readonly ReadOnlyCollection<int> ExperienceTree = Array.AsReadOnly(
        new[]
        {
            825,
            825 * 2,
            825 * 3,
            825 * 4,
            825 * 5,
            825 * 6,
            825 * 7,
            825 * 8,
            825 * 8 + 900,
        }
    );

    /// <summary>
    /// 功率值
    /// 英雄机器人 - 功率优先
    ///
    /// 表 5-16 英雄机器人底盘属性
    /// 等级 上限血量 底盘功率上限（W）
    /// 1 200 70
    /// 2 225 75
    /// 3 250 80
    /// 4 275 85
    /// 5 300 90
    /// 6 325 95
    /// 7 350 100
    /// 8 375 105
    /// 9 400 110
    /// 10 500 120
    /// </summary>
    private static readonly ReadOnlyCollection<int> PowerTreeHeroPowerPriority =
        Array.AsReadOnly(new[] { 70, 75, 80, 85, 90, 95, 100, 105, 110, 120 });

    /// <summary>
    /// 血量值
    ///
    /// 英雄机器人 - 功率优先
    /// </summary>
    private static readonly ReadOnlyCollection<uint> HealthTreeHeroPowerPriority =
        Array.AsReadOnly(new uint[] { 200, 225, 250, 275, 300, 325, 350, 375, 400, 500 });

    /// <summary>
    /// 功率值
    /// 英雄机器人 - 血量优先
    ///
    /// 表 5-16 英雄机器人底盘属性
    /// 等级 上限血量 底盘功率上限（W）
    /// 1 250 55
    /// 2 275 60
    /// 3 300 65
    /// 4 325 70
    /// 5 350 75
    /// 6 375 80
    /// 7 400 85
    /// 8 425 90
    /// 9 450 100
    /// 10 500 120
    /// </summary>
    private static readonly ReadOnlyCollection<int> PowerTreeHeroHealthPriority =
        Array.AsReadOnly(new[] { 55, 60, 65, 70, 75, 80, 85, 90, 100, 120 });

    /// <summary>
    /// 血量值
    ///
    /// 英雄机器人 - 血量优先
    /// </summary>
    private static readonly ReadOnlyCollection<uint> HealthTreeHeroHealthPriority =
        Array.AsReadOnly(new uint[] { 250, 275, 300, 325, 350, 375, 400, 425, 450, 500 });

    /// <summary>
    /// 功率值
    /// 步兵机器人 - 功率优先
    ///
    /// 表 5-17 步兵机器人底盘属性
    /// 等级 上限血量 底盘功率上限（W）
    /// 1 150 60
    /// 2 175 65
    /// 3 200 70
    /// 4 225 75
    /// 5 250 80
    /// 6 275 85
    /// 7 300 90
    /// 8 325 95
    /// 9 350 100
    /// 10 400 100
    /// </summary>
    private static readonly ReadOnlyCollection<int> PowerTreeInfantryPowerPriority =
        Array.AsReadOnly(new[] { 60, 65, 70, 75, 80, 85, 90, 95, 100, 100 });

    /// <summary>
    /// 血量值
    /// 步兵机器人 - 功率优先
    /// </summary>
    private static readonly ReadOnlyCollection<uint> HealthTreeInfantryPowerPriority =
        Array.AsReadOnly(new uint[] { 150, 175, 200, 225, 250, 275, 300, 325, 350, 400 });

    /// <summary>
    /// 功率值
    /// 步兵机器人 - 血量优先
    ///
    /// 表 5-17 步兵机器人底盘属性
    /// 等级 上限血量 底盘功率上限（W）
    /// 1 200 45
    /// 2 225 50
    /// 3 250 55
    /// 4 275 60
    /// 5 300 65
    /// 6 325 70
    /// 7 350 75
    /// 8 375 80
    /// 9 400 90
    /// 10 400 100
    /// </summary>
    private static readonly ReadOnlyCollection<int> PowerTreeInfantryHealthPriority =
        Array.AsReadOnly(new[] { 45, 50, 55, 60, 65, 70, 75, 80, 90, 100 });

    /// <summary>
    /// 血量值
    ///
    /// 步兵机器人 - 血量优先
    /// </summary>
    private static readonly ReadOnlyCollection<uint> HealthTreeInfantryHealthPriority =
        Array.AsReadOnly(new uint[] { 200, 225, 250, 275, 300, 325, 350, 375, 400, 400 });

    /// <summary>
    /// 42mm 发射机构属性
    /// 热量上限
    ///
    /// 等级 射击热量上限 射击热量冷却速率（每秒）
    /// 1 200 40
    /// 2 230 48
    /// 3 260 56
    /// 4 290 64
    /// 5 320 72
    /// 6 350 80
    /// 7 380 88
    /// 8 410 96
    /// 9 440 104
    /// 10 500 120
    /// </summary>
    private static readonly ReadOnlyCollection<float> HeatTree42mmDefault = Array.AsReadOnly(
        new float[] { 200, 230, 260, 290, 320, 350, 380, 410, 440, 500 }
    );

    /// <summary>
    /// 42mm 发射机构属性
    /// 冷却速率
    /// </summary>
    private static readonly ReadOnlyCollection<int> CooldownTree42mmDefault = Array.AsReadOnly(
        new int[] { 40, 48, 56, 64, 72, 80, 88, 96, 104, 120 }
    );

    /// <summary>
    /// 17mm发射机构属性 - 爆发优先
    /// 热量上限
    ///
    /// 表 5-18 17mm 发射机构属性
    /// 等级 射击热量上限 射击热量冷却速率（每秒）
    /// 1 200 10
    /// 2 250 15
    /// 3 300 20
    /// 4 350 25
    /// 5 400 30
    /// 6 450 35
    /// 7 500 40
    /// 8 550 45
    /// 9 600 50
    /// 10 650 60
    /// </summary>
    private static readonly ReadOnlyCollection<float> HeatTree17mmBurst = Array.AsReadOnly(
        new float[] { 200, 250, 300, 350, 400, 450, 500, 550, 600, 650 }
    );

    /// <summary>
    /// 17mm发射机构属性 - 爆发优先
    /// 冷却速率
    /// </summary>
    private static readonly ReadOnlyCollection<int> CooldownTree17mmBurst = Array.AsReadOnly(
        new int[] { 10, 15, 20, 25, 30, 35, 40, 45, 50, 60 }
    );

    /// <summary>
    /// 17mm发射机构属性 - 冷却优先
    /// 热量上限
    ///
    /// 等级 射击热量上限 射击热量冷却速率（每秒）
    /// 1 50 40
    /// 2 85 45
    /// 3 120 50
    /// 4 155 55
    /// 5 190 60
    /// 6 225 65
    /// 7 260 70
    /// 8 295 75
    /// 9 330 80
    /// 10 400 80
    /// </summary>
    private static readonly ReadOnlyCollection<float> HeatTree17mmCooldown = Array.AsReadOnly(
        new float[] { 50, 85, 120, 155, 190, 225, 260, 295, 330, 400 }
    );

    /// <summary>
    /// 17mm发射机构属性 - 冷却优先
    /// 冷却速率
    /// </summary>
    private static readonly ReadOnlyCollection<int> CooldownTree17mmCooldown = Array.AsReadOnly(
        new int[] { 40, 45, 50, 55, 60, 65, 70, 75, 80, 80 }
    );

    /*
     * 难度等级 兑换银矿石可获得基础金币数量 兑换金矿石可获得基础金币数量
一级 75 225
二级 100 275
三级 175 350
四级 300 500
     */
    private static readonly ReadOnlyCollection<int> BaseCoinTreeSilver = Array.AsReadOnly(
        new[] { 75, 100, 175, 300 }
    );

    private static readonly ReadOnlyCollection<int> BaseCoinTreeGold = Array.AsReadOnly(
        new[] { 225, 275, 350, 500 }
    );

    /// <summary>
    /// 获取兑换基础金币数量
    /// </summary>
    /// <returns></returns>
    public int GetExchangeBaseCoin(int level, OreType oreType)
    {
        return oreType switch
        {
            OreType.Silver => BaseCoinTreeSilver[level - 1],
            OreType.Gold => BaseCoinTreeGold[level - 1],
            _ => throw new ArgumentOutOfRangeException(nameof(oreType), oreType, null),
        };
    }

    public override int GetStageTimeLimit(JudgeSystemStage stage)
    {
        return stage switch
        {
            JudgeSystemStage.Repair => 60,
            JudgeSystemStage.SelfCheck => 10,
            JudgeSystemStage.Countdown => 5,
            JudgeSystemStage.Match => 7 * 60,
            JudgeSystemStage.Settlement => 10,
            _ => int.MaxValue,
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
            Engineer => EngineerMaxHealth,
            Sentry => SentryMaxHealth,
            Base => BaseMaxHealth,
            Outpost => OutpostMaxHealth,
            _ => MaxHealthFallback,
        };
    }

    public override float GetMaxHeat(IShooter shooter)
    {
        if (shooter is Sentry)
            return SentryMaxHeat;
        if (shooter is Aerial)
            return float.MaxValue;
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
        if (shooter.IsHero())
            return HeroMaxBulletSpeed;
        if (shooter.IsInfantry() || shooter.IsAerial() || shooter.IsSentry())
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