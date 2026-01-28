using System.Linq;
using RoboSouls.JudgeSystem.Entities;
using RoboSouls.JudgeSystem.Events;
using RoboSouls.JudgeSystem.RoboMaster2025UC.Entities;
using RoboSouls.JudgeSystem.Systems;
using VContainer;
using VitalRouter;

namespace RoboSouls.JudgeSystem.RoboMaster2025UC.Systems;

/// <summary>
///     经验值分发系统
/// </summary>
[Routes]
public sealed partial class ExperienceDispatchSystem : ISystem
{
    [Inject] internal ExperienceSystem ExperienceSystem { get; set; }

    [Inject] internal EntitySystem EntitySystem { get; set; }

    [Inject] internal ILogger Logger { get; set; }

    [Inject] internal ITimeSystem TimeSystem { get; set; }

    [Inject] internal RM2025ucPerformanceSystem PerformanceSystem { get; set; }

    [Inject] internal LifeSystem LifeSystem { get; set; }

    [Inject]
    internal void Inject(Router router)
    {
        MapTo(router);
    }

    /// <summary>
    ///     发射弹丸
    ///      步兵机器人：每发射 1 发弹丸，获得 1 点经验
    ///      英雄机器人：每发射 1 发弹丸，获得 10 点经验
    /// </summary>
    /// <param name="shooter"></param>
    /// <param name="amount"></param>
    [Route]
    private void OnShoot(ShootCommand command)
    {
        if (TimeSystem.Stage != JudgeSystemStage.Match)
            return;

        if (command.Shooter is Hero h)
            ExperienceSystem.AddExp(h, command.Amount * 10);
        else if (command.Shooter is Infantry r) ExperienceSystem.AddExp(r, command.Amount);
    }

    /// <summary>
    ///     造成攻击伤害
    /// </summary>
    /// <param name="damageCommand"></param>
    [Route]
    private void OnDamage(DamageCommand damageCommand)
    {
        if (TimeSystem.Stage != JudgeSystemStage.Match)
            return;
        if (damageCommand.Shooter is not IExperienced shooter)
            return;

        var deltaExp = 0;
        // 对机器人造成攻击伤害，每造成 1 点伤害，攻击方获得 4 点经验
        if (damageCommand.Victim is IRobot) deltaExp = (int)(damageCommand.Damage * 4);

        // 对基地顶部大装甲模块造成17mm攻击伤害，每造成 1 点伤害，攻击方获得 2 点经验
        if (
            damageCommand.AmmoType == PerformanceSystemBase.AmmoType17mm
            && damageCommand.Victim is Base
            && damageCommand.ArmorId == 0
        )
            deltaExp = (int)(damageCommand.Damage * 2);
        else if (damageCommand.Victim is Base or Outpost) deltaExp = (int)damageCommand.Damage;

        // 对基地、前哨造成42mm攻击伤害
        if (damageCommand.AmmoType == PerformanceSystemBase.AmmoType42mm) deltaExp = (int)damageCommand.Damage;

        ExperienceSystem.AddExp(shooter, deltaExp);
    }

    /// <summary>
    ///     机器人战亡
    ///      若击毁者为英雄机器人且导致战亡的伤害类型为 42mm 弹丸伤害：
    ///     > 当被击毁者等级大于等于击毁者等级时，经验计算方式如下：
    ///     击毁者所获得的经验=50*被击毁者等级*（1+0.2*被击毁者与击毁者等级差）
    ///     > 当被击毁者等级小于击毁者等级时，被击毁者与击毁者等级差视为 0，经验计算方式如下：
    ///     击毁者所获得的经验=50*被击毁者等级
    ///      若导致战亡的伤害类型不为 42mm 弹丸伤害：
    ///     击毁者等级视为另一方存活步兵机器人的平均经验所对应的等级。平均经验取
    ///     四舍五入后的值。
    ///     机器人因装甲模块被攻击外的其他原因导致变为非存活状态或裁判系统无法检测到
    ///     击毁者时，均视为找不到击毁者。
    /// </summary>
    /// <param name="evt"></param>
    [Route]
    private void OnKill(KillEvent evt)
    {
        if (!EntitySystem.TryGetEntity(evt.Victim, out IRobot victim))
            return;
        if (!EntitySystem.TryGetEntity(evt.Killer, out IExperienced killer))
            return;

        var killerLevel = PerformanceSystem.GetLevel(killer);
        var victimLevel = victim is IExperienced exp ? PerformanceSystem.GetLevel(exp) : 1;
        if (killer is not Hero)
        {
            var robots = EntitySystem
                .GetOperatedEntities<Infantry>(victim.Id.Camp)
                .Where(r => !r.IsDead())
                .ToList();
            if (robots.Count == 0)
            {
                killerLevel = 1;
            }
            else
            {
                var avgExp = robots.Sum(r => r.Experience) / robots.Count;
                killerLevel = RM2025ucPerformanceSystem.GetLevel(avgExp);
            }
        }

        var levelDiff = victimLevel - killerLevel;
        if (levelDiff < 0)
            levelDiff = 0;

        var expGained = 50 * victimLevel * (1 + 0.2 * levelDiff);
        ExperienceSystem.AddExp(killer, (int)expGained);
    }

    [Route]
    private void OnLevelUp(LevelUpdateEvent evt)
    {
        if (evt.PrevLevel >= evt.NewLevel || evt.PrevLevel <= 1)
            return;
        if (!EntitySystem.TryGetOperatedEntity(evt.Operator, out IHealthed healthed))
            return;

        var maxHealth = PerformanceSystem.GetMaxHealth(healthed);
        var lastMaxHealth = PerformanceSystem.GetMaxHealth(healthed, evt.PrevLevel);
        var deltaHealth = maxHealth - lastMaxHealth;

        LifeSystem.IncreaseHealth(healthed, deltaHealth);
    }
}