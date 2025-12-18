namespace RoboSouls.JudgeSystem;

public static class Buffs
{
    /// <summary>
    /// 防御增益
    /// </summary>
    public static readonly int DefenceBuff = "defense".Sum();

    /// <summary>
    /// 攻击增益
    /// </summary>
    public static readonly int AttackBuff = "attack".Sum();

    /// <summary>
    /// 脱战增益
    /// </summary>
    public static readonly int OutOfCombatBuff = "out_of_combat".Sum();

    /// <summary>
    /// 射击热量冷却增益
    /// </summary>
    public static readonly int CoolDownBuff = "cool_down".Sum();

    /// <summary>
    /// 值的射击热量冷却增益
    /// </summary>
    public static readonly int CoolDownAmountBuff = "cool_down_amount".Sum();

    /// <summary>
    /// 缓冲能量增益
    ///
    /// 与原规则缓冲能量增加值 xx J不同，设计为持续较短时间的倍率增益
    /// </summary>
    public static readonly int PowerBuff = "power".Sum();

    /// <summary>
    /// 底盘断电
    /// </summary>
    public static readonly int ChassisPowerOffBuff = "chassis_power_off".Sum();

    public static readonly int GimbalPowerOffBuff = "gimbal_power_off".Sum();

    /// <summary>
    /// 红牌罚下
    /// </summary>
    public static readonly int RedCard = "red_card".Sum();

    /// <summary>
    /// 黄牌警告
    /// </summary>
    public static readonly int YellowCard = "yellow_card".Sum();

    /// <summary>
    /// 黄牌警告队友
    /// </summary>
    public static readonly int YellowCardTeammate = "yellow_card_teammate".Sum();

    /// <summary>
    /// 第一人称可视度降低
    /// </summary>
    /// <returns></returns>
    public static readonly int FpvVisibilityReduced = "fpv_visibility_reduced".Sum();

    /// <summary>
    /// 发射机构锁定
    /// </summary>
    public static readonly int GunLocked = "gun_locked".Sum();
}