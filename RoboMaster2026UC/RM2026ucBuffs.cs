namespace RoboSouls.JudgeSystem.RoboMaster2026UC;

public static class RM2026ucBuffs
{
    public static readonly int SmallPowerRuneBuff = "small_power_rune".Sum();
    public static readonly int BigPowerRuneBuff = "big_power_rune".Sum();
    public static readonly int DartHitBuff = "dart_hit".Sum();
    public static readonly int TerrainLeapBuff = "terrain_leap".Sum();
    public static readonly int TerrainLeapRoadBuff = "terrain_leap_road".Sum();
    public static readonly int TerrainLeapOverSlopeBuff = "terrain_leap_over_slope".Sum();
    public static readonly int TerrainLeapHighlandBuff = "terrain_leap_highland".Sum();
    public static readonly int FortressBuff = "fortress".Sum();
    public static readonly int HeroDeploymentModeBuff = "hero_deployment_mode".Sum();
    public static readonly int WeakenedBuff = "weakened".Sum();

    /// <summary>
    /// 热量超限导致的发射机构锁定
    /// </summary>
    public static readonly int HeatGunLocked = "head_gun_locked".Sum();

    /// <summary>
    /// 易伤
    /// </summary>
    public static readonly int Vulnerable = "vulnerable".Sum();
}