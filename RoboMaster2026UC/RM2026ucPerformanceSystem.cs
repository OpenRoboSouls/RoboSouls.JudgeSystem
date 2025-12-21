using System;
using System.Collections.ObjectModel;
using RoboSouls.JudgeSystem.Entities;
using RoboSouls.JudgeSystem.RoboMaster2026UC.Entities;
using RoboSouls.JudgeSystem.Systems;
using VContainer;

namespace RoboSouls.JudgeSystem.RoboMaster2026UC;

public sealed class RM2026ucPerformanceSystem : PerformanceSystemBase
{
    /// <summary>
    /// 近战优先
    /// </summary>
    public const byte ChassisTypeHeroMelee = 3;

    /// <summary>
    /// 远程优先
    /// </summary>
    public const byte ChassisTypeHeroSniper = 4;
        
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
    /// 表 5-12 英雄、步兵、空中机器人的等级和经验
    /// </summary>
    private static readonly ReadOnlyCollection<int> ExperienceTree = Array.AsReadOnly(
        [
            550,
            1100,
            1650,
            2200,
            2750,
            3300,
            3850,
            4400,
            5000
        ]
    );

    /// <summary>
    /// 功率值
    /// 英雄机器人 - 近战优先
    /// </summary>
    private static readonly ReadOnlyCollection<int> PowerTreeHeroMeleeFocused =
        Array.AsReadOnly([70, 75, 80, 85, 90, 95, 100, 105, 110, 120]);

    /// <summary>
    /// 血量值
    ///
    /// 英雄机器人 - 近战优先
    /// </summary>
    private static readonly ReadOnlyCollection<uint> HealthTreeHeroMeleeFocused =
        Array.AsReadOnly(new uint[] { 200, 225, 250, 275, 300, 325, 350, 375, 400, 450 });
        
    /// <summary>
    /// 射击热量上限
    ///
    /// 英雄机器人 - 近战优先
    /// </summary>
    private static readonly ReadOnlyCollection<float> HeatTreeHeroMeleeFocused =
        Array.AsReadOnly(new float[] { 140, 150, 160, 170, 180, 190, 200, 210, 220, 240 });
        
    /// <summary>
    /// 射击热量冷却速率
    ///
    /// 英雄机器人 - 近战优先
    /// </summary>
    private static readonly ReadOnlyCollection<int> CooldownTreeHeroMeleeFocused =
        Array.AsReadOnly([12, 14, 16, 18, 20, 22, 24, 26, 28, 30]);

    /// <summary>
    /// 功率值
    /// 
    /// 英雄机器人 - 远程优先
    /// </summary>
    private static readonly ReadOnlyCollection<int> PowerTreeHeroSniperFocused =
        Array.AsReadOnly([50, 55, 60, 65, 70, 75, 80, 85, 90, 100]);

    /// <summary>
    /// 血量值
    ///
    /// 英雄机器人 - 远程优先
    /// </summary>
    private static readonly ReadOnlyCollection<uint> HealthTreeHeroSniperFocused =
        Array.AsReadOnly(new uint[] { 150, 165, 180, 195, 210, 225, 240, 255, 270, 300 });
        
    /// <summary>
    /// 射击热量上限
    ///
    /// 英雄机器人 - 远程优先
    /// </summary>
    private static readonly ReadOnlyCollection<float> HeatTreeHeroSniperFocused =
        Array.AsReadOnly(new float[] { 100, 102, 104, 106, 108, 110, 115, 120, 125, 130 });
        
    /// <summary>
    /// 射击热量冷却速率
    ///
    /// 英雄机器人 - 远程优先
    /// </summary>
    private static readonly ReadOnlyCollection<int> CooldownTreeHeroSniperFocused =
        Array.AsReadOnly([20, 23, 26, 29, 32, 35, 38, 41, 44, 50]);

    /// <summary>
    /// 功率值
    /// 
    /// 步兵机器人 - 功率优先
    /// </summary>
    private static readonly ReadOnlyCollection<int> PowerTreeInfantryPowerPriority =
        Array.AsReadOnly([60, 65, 70, 75, 80, 85, 90, 95, 100, 100]);

    /// <summary>
    /// 血量值
    /// 
    /// 步兵机器人 - 功率优先
    /// </summary>
    private static readonly ReadOnlyCollection<uint> HealthTreeInfantryPowerPriority =
        Array.AsReadOnly(new uint[] { 150, 175, 200, 225, 250, 275, 300, 325, 350, 400 });

    /// <summary>
    /// 功率值
    /// 
    /// 步兵机器人 - 血量优先
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
    /// 17mm发射机构属性 - 爆发优先
    /// 
    /// 热量上限
    /// </summary>
    private static readonly ReadOnlyCollection<float> HeatTree17mmBurst = Array.AsReadOnly(
        new float[] { 170, 180, 190, 200, 210, 220, 230, 240, 250, 260 }
    );

    /// <summary>
    /// 17mm发射机构属性 - 爆发优先
    /// 
    /// 冷却速率
    /// </summary>
    private static readonly ReadOnlyCollection<int> CooldownTree17mmBurst = Array.AsReadOnly(
        [5, 7, 9, 11, 12, 13, 14, 16, 18, 20]
    );

    /// <summary>
    /// 17mm发射机构属性 - 冷却优先
    /// 
    /// 热量上限
    /// </summary>
    private static readonly ReadOnlyCollection<float> HeatTree17mmCooldown = Array.AsReadOnly(
        new float[] { 40, 48, 56, 64, 72, 80, 88, 96, 114, 120 }
    );

    /// <summary>
    /// 17mm发射机构属性 - 冷却优先
    /// 
    /// 冷却速率
    /// </summary>
    private static readonly ReadOnlyCollection<int> CooldownTree17mmCooldown = Array.AsReadOnly(
        [12, 14, 16, 18, 20, 22, 24, 26, 28, 90]
    );

    /// <summary>
    /// 17mm发射机构属性 - 空中机器人
    ///
    /// 热量上限
    /// </summary>
    private static readonly ReadOnlyCollection<float> HeatTree17mmAerial = Array.AsReadOnly(
        new float[] { 100, 110, 120, 130, 140, 150, 160, 170, 180, 200 }
    );
        
    /// <summary>
    /// 17mm发射机构属性 - 空中机器人
    ///
    /// 冷却速率
    /// </summary>
    private static readonly ReadOnlyCollection<int> CooldownTree17mmAerial = Array.AsReadOnly(
        [20, 30, 40, 50, 60, 70, 80, 90, 100, 120]
    );

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
        
    [Inject] internal ICacheProvider<int> IntCacheBox { get; set; }

    private const ushort LevelUpperBoundId = 0x3001;
        
    public int GetLevelUpperBound(Camp camp)
    {
        return IntCacheBox.WithReaderNamespace(new Identity(camp, LevelUpperBoundId)).LoadOrDefault(0, 5);
    }
        
    public void SetLevelUpperBound(Camp camp, int level)
    {
        IntCacheBox.WithWriterNamespace(new Identity(camp, LevelUpperBoundId)).Save(0, level);
    }

    public override int GetLevel(IExperienced experienced)
    {
        var upperBound = GetLevelUpperBound(experienced.Id.Camp);
            
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
                ChassisTypeHeroMelee => PowerTreeHeroMeleeFocused[GetLevel(h) - 1],
                ChassisTypeHeroSniper => PowerTreeHeroSniperFocused[GetLevel(h) - 1],
                _ => throw new ArgumentException($"Unknown chassis type: {chassisd.ChassisType}"),
            },
            Infantry i => chassisd.ChassisType switch
            {
                ChassisTypePower => PowerTreeInfantryPowerPriority[GetLevel(i) - 1],
                ChassisTypeHealth => PowerTreeInfantryHealthPriority[GetLevel(i) - 1],
                _ => throw new ArgumentException($"Unknown chassis type: {chassisd.ChassisType}"),
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
                ChassisTypeHeroMelee => PowerTreeHeroMeleeFocused[0],
                ChassisTypeHeroSniper => PowerTreeHeroSniperFocused[0],
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
                ChassisTypeHeroMelee => HealthTreeHeroMeleeFocused[level - 1],
                ChassisTypeHeroSniper => HealthTreeHeroSniperFocused[level - 1],
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
        if (shooter is not IExperienced exp)
            return 0;
        var level = GetLevel(exp);

        if (shooter is Hero h)
        {
            return h.ChassisType switch
            {
                ChassisTypeHeroMelee => HeatTreeHeroMeleeFocused[level - 1],
                ChassisTypeHeroSniper => HeatTreeHeroSniperFocused[level - 1],
                _ => throw new ArgumentException($"Unknown chassis type: {h.ChassisType}"),
            };
        }

        if (shooter is Infantry i)
        {
            return i.GunType switch
            {
                GunType17mmBurst => HeatTree17mmBurst[level - 1],
                GunType17mmCooldown => HeatTree17mmCooldown[level - 1],
                _ => throw new ArgumentException($"Unknown gun type: {shooter.GunType}"),
            };
        }

        if (shooter is Aerial a)
        {
            return HeatTree17mmAerial[level - 1];
        }
            
        return 0;
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

        if (shooter is Hero h)
        {
            return h.ChassisType switch
            {
                ChassisTypeHeroMelee => CooldownTreeHeroMeleeFocused[level - 1],
                ChassisTypeHeroSniper => CooldownTreeHeroSniperFocused[level - 1],
                _ => throw new ArgumentException($"Unknown chassis type: {h.ChassisType}"),
            };
        }

        if (shooter is Infantry i)
        {
            return i.GunType switch
            {
                GunType17mmBurst => CooldownTree17mmBurst[level - 1],
                GunType17mmCooldown => CooldownTree17mmCooldown[level - 1],
                _ => throw new ArgumentException($"Unknown gun type: {shooter.GunType}"),
            };
        }
            
        if (shooter is Aerial a)
        {
            return CooldownTree17mmAerial[level - 1];
        }

        return 0;
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