using RoboSouls.JudgeSystem.Entities;

namespace RoboSouls.JudgeSystem.Systems;

/// <summary>
///     性能体系管理
///     仅作为数值提供，不应包含逻辑处理
/// </summary>
public abstract class PerformanceSystemBase
{
    public const byte AmmoTypeNone = 0;

    /// <summary>
    ///     17mm小弹丸
    /// </summary>
    public const byte AmmoType17mm = 1;

    /// <summary>
    ///     42mm大弹丸
    /// </summary>
    public const byte AmmoType42mm = 2;

    /// <summary>
    ///     功率优先
    /// </summary>
    public const byte ChassisTypePower = 1;

    /// <summary>
    ///     血量优先
    /// </summary>
    public const byte ChassisTypeHealth = 2;


    public const byte ChassisTypeHero = 3;

    /// <summary>
    ///     爆发优先
    /// </summary>
    public const byte GunType17mmBurst = 1;

    /// <summary>
    ///     冷却优先
    /// </summary>
    public const byte GunType17mmCooldown = 2;

    public const byte GunType42mmDefault = 3;
    public const byte ArmorTypeSmall = 1;
    public const byte ArmorTypeLarge = 2;

    public virtual int MaxYellowCardCount => 4;

    public abstract int GetStageTimeLimit(JudgeSystemStage stage);
    public abstract int GetLevel(IExperienced experienced);
    public abstract int GetLevelExpLength(IExperienced experienced);
    public abstract int GetLevelExpGained(IExperienced experienced);
    public abstract int GetMaxPower(IChassisd chassisd);
    public abstract int GetBasePower(IChassisd chassisd);
    public abstract uint GetMaxHealth(IHealthed healthed, int level = -1);
    public abstract float GetMaxHeat(IShooter shooter);
    public abstract int GetHeatDelta(IShooter shooter);
    public abstract int GetCooldown(IShooter shooter);
    public abstract int GetMaxBulletSpeed(in Identity id);
}