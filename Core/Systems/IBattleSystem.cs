using RoboSouls.JudgeSystem.Entities;

namespace RoboSouls.JudgeSystem.Systems;

public interface IBattleSystem : ISystem
{
    public bool TryShoot(IShooter shooter, int count);

    public void OnArmorHit(
        in Identity attacker,
        in Identity victim,
        byte ammoType,
        byte armorType,
        byte armorId
    );
}

public struct DamageRecord;