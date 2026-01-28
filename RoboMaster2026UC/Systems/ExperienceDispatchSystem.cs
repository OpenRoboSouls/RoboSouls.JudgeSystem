using System;
using System.Linq;
using RoboSouls.JudgeSystem.Entities;
using RoboSouls.JudgeSystem.Events;
using RoboSouls.JudgeSystem.RoboMaster2026UC.Entities;
using RoboSouls.JudgeSystem.Systems;
using VContainer;
using VitalRouter;

namespace RoboSouls.JudgeSystem.RoboMaster2026UC.Systems;

/// <summary>
///     经验值分发系统
/// </summary>
[Routes]
public sealed partial class ExperienceDispatchSystem(
    ExperienceSystem experienceSystem,
    EntitySystem entitySystem,
    ITimeSystem timeSystem,
    RM2026ucPerformanceSystem performanceSystem,
    LifeSystem lifeSystem)
    : ISystem
{
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
        if (timeSystem.Stage != JudgeSystemStage.Match)
            return;

        if (command.Shooter is Hero h)
            experienceSystem.AddExp(h, command.Amount * 10);
        else if (command.Shooter is Infantry r) experienceSystem.AddExp(r, command.Amount);
    }

    /// <summary>
    ///     造成攻击伤害
    /// </summary>
    /// <param name="damageCommand"></param>
    [Route]
    private void OnDamage(DamageCommand damageCommand)
    {
        if (timeSystem.Stage != JudgeSystemStage.Match)
            return;
        if (damageCommand.Shooter is not IExperienced shooter)
            return;

        var deltaExp = 0;
        // 对机器人造成攻击伤害，每造成 1 点伤害，攻击方获得 4 点经验
        if (damageCommand.Victim is IRobot) deltaExp = (int)(damageCommand.Damage * 4);

        // 对前哨站装甲模块造成攻击伤害： 每造成 1 点伤害，攻击方获得 2 点经验
        if (damageCommand.Victim is Outpost) deltaExp = (int)(damageCommand.Damage * 2);

        // 对基地装甲模块造成攻击伤害： 每造成 2 点伤害，攻击方获得 1 点经验
        if (damageCommand.Victim is Base) deltaExp = (int)(damageCommand.Damage / 2);

        experienceSystem.AddExp(shooter, deltaExp);
    }

    /// <summary>
    ///     若存在击毁者且击毁者可以获取经验：
    ///      当被击毁者等级大于等于击毁者等级时，经验计算方式如下：
    ///     击毁者所获得的经验=50*被击毁者等级*（1+0.2*被击毁者与击毁者等
    ///     级差）
    ///      当被击毁者等级小于击毁者等级时，被击毁者与击毁者等级差视为 0，
    ///     经验计算方式如下：
    ///     击毁者所获得的经验=50*被击毁者等级
    ///      若裁判系统未检测到击毁者或击毁者不能获取经验：
    ///     击毁者等级视为另一方存活英雄、 步兵、空中机器人的平均经验所对应的等级。
    ///     按照上文中公式计算经验后，平分给另一方存活的英雄、 步兵、空中机器人，平
    ///     均经验取整数值。
    ///     机器人因装甲模块被攻击外的其他原因导致变为战亡、异常离线或裁判系统无法
    ///     检测到击毁者时，均视为找不到击毁者。
    /// </summary>
    /// <param name="evt"></param>
    [Route]
    private void OnKill(KillEvent evt)
    {
        if (!entitySystem.TryGetEntity(evt.Victim, out IRobot victim))
            return;
        if (!entitySystem.TryGetEntity(evt.Killer, out IRobot killer))
            return;

        var victimLevel = victim is IExperienced ve ? performanceSystem.GetLevel(ve) : 1;
        if (killer is IExperienced ke)
        {
            var killerLevel = performanceSystem.GetLevel(ke);

            experienceSystem.AddExp(ke, CalcExpGained(victimLevel, killerLevel));
        }
        else
        {
            var averageRobots = entitySystem.GetOperatedEntities<IExperienced>(evt.Killer.Camp)
                .Where(r => r is IHealthed h && !h.IsDead())
                .ToArray();
            var averageExp = averageRobots.Average(r => r.Experience);
            var killerLevel = RM2026ucPerformanceSystem.GetLevel((int)Math.Round(averageExp));
            var exp = CalcExpGained(victimLevel, killerLevel) / averageRobots.Length;

            foreach (var robot in averageRobots) experienceSystem.AddExp(robot, exp);
        }
    }

    private static int CalcExpGained(int victimLevel, int killerLevel)
    {
        float expGained = 0;
        if (victimLevel >= killerLevel)
            expGained = 50f * victimLevel * (1f + 0.2f * (victimLevel - killerLevel));
        else
            expGained = 50f * victimLevel;

        return (int)MathF.Round(expGained);
    }

    [Route]
    private void OnLevelUp(LevelUpdateEvent evt)
    {
        if (evt.PrevLevel >= evt.NewLevel || evt.PrevLevel <= 1)
            return;
        if (!entitySystem.TryGetOperatedEntity(evt.Operator, out IHealthed healthed))
            return;

        var maxHealth = performanceSystem.GetMaxHealth(healthed);
        var lastMaxHealth = performanceSystem.GetMaxHealth(healthed, evt.PrevLevel);
        var deltaHealth = maxHealth - lastMaxHealth;

        lifeSystem.IncreaseHealth(healthed, deltaHealth);
    }
}