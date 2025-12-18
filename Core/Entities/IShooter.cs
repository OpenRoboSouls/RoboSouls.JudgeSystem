namespace RoboSouls.JudgeSystem.Entities;

public interface IShooter : IEntity
{
    /// <summary>
    /// 允许发弹量
    /// </summary>
    /// <returns></returns>
    public static readonly int AmmoAllowanceCacheKey = "ammo_allowance".Sum();

    public static readonly int HeatCacheKey = "heat".Sum();
    public static readonly int GunTypeCacheKey = "GunType".Sum();
    public int AmmoAllowance { get; }
    public float Heat { get; }

    public byte AmmoType { get; }
    public byte GunType { get; }
}