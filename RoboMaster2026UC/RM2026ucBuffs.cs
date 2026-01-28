using RoboSouls.JudgeSystem.Attributes;

namespace RoboSouls.JudgeSystem.RoboMaster2026UC;

public static partial class RM2026ucBuffs
{
    [Hashed]public static partial int SmallPowerRuneBuff { get; }
    [Hashed]public static partial int BigPowerRuneBuff { get; }
    [Hashed]public static partial int DartHitBuff { get; }
    [Hashed]public static partial int TerrainLeapBuff { get; }
    [Hashed]public static partial int TerrainLeapRoadBuff { get; }
    [Hashed]public static partial int TerrainLeapOverSlopeBuff { get; }
    [Hashed]public static partial int TerrainLeapHighlandBuff { get; }
    [Hashed]public static partial int FortressBuff { get; }
    [Hashed]public static partial int HeroDeploymentModeBuff { get; }
    [Hashed]public static partial int WeakenedBuff { get; }

    /// <summary>
    /// 热量超限导致的发射机构锁定
    /// </summary>
    [Hashed]
    public static partial int HeatGunLocked { get; }

    /// <summary>
    /// 易伤
    /// </summary>
    [Hashed]
    public static partial int Vulnerable { get; }
    
    /// <summary>
    /// 永久防御增益
    /// </summary>
    [Hashed]
    public static partial int PermanentDefense { get; }

    /// <summary>
    /// 空中机器人被雷达锁定
    /// </summary>
    [Hashed]
    public static partial int RadarLock { get; }

    /// <summary>
    /// 空中机器人被雷达压制
    /// </summary>
    [Hashed]
    public static partial int RadarCountered { get; }
    
    /// <summary>
    /// 正在重建前哨站
    /// </summary>
    [Hashed]
    public static partial int RebuildingOutpost { get; }
}