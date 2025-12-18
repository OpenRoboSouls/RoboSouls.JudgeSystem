using System.Linq;
using RoboSouls.JudgeSystem.Entities;
using RoboSouls.JudgeSystem.Events;
using RoboSouls.JudgeSystem.RoboMaster2025UL.Entities;
using RoboSouls.JudgeSystem.Systems;
using VContainer;
using VitalRouter;

namespace RoboSouls.JudgeSystem.RoboMaster2025UL.Systems;

/// <summary>
/// 经验值分发系统
/// </summary>
[Routes]
public sealed partial class ExperienceDispatchSystem : ISystem
{
    [Inject]
    internal void Inject(Router router)
    {
        MapTo(router);
    }

    public ExperienceDispatchSystem(BattleSystem battleSystem)
    {
        battleSystem.OnShoot += OnShoot;
    }

    [Inject]
    internal ExperienceSystem ExperienceSystem { get; set; }

    [Inject]
    internal EntitySystem EntitySystem { get; set; }

    [Inject]
    internal ILogger Logger { get; set; }

    [Inject]
    internal ITimeSystem TimeSystem { get; set; }

    [Inject]
    internal RM2025ulPerformanceSystem PerformanceSystem { get; set; }

    /// <summary>
    /// 发射弹丸
    ///  步兵机器人：每发射 1 发弹丸，获得 1 点经验
    ///  英雄机器人：每发射 1 发弹丸，获得 10 点经验
    /// </summary>
    /// <param name="shooter"></param>
    /// <param name="amount"></param>
    private void OnShoot(IShooter shooter, int amount)
    {
        if (TimeSystem.Stage != JudgeSystemStage.Match)
            return;

        if (shooter is Hero h)
        {
            ExperienceSystem.AddExp(h, amount * 10);
        }
        else if (shooter is Infantry r)
        {
            ExperienceSystem.AddExp(r, amount);
        }
    }

    /// <summary>
    /// 机器人战亡
    ///  若击毁者为英雄机器人且导致战亡的伤害类型为 42mm 弹丸伤害：
    /// > 当被击毁者等级大于等于击毁者等级时，经验计算方式如下：
    ///     击毁者所获得的经验=50*被击毁者等级*（1+0.2*被击毁者与击毁者等级差）
    /// > 当被击毁者等级小于击毁者等级时，被击毁者与击毁者等级差视为 0，经验计算方式如下：
    ///     击毁者所获得的经验=50*被击毁者等级
    ///  若导致战亡的伤害类型不为 42mm 弹丸伤害：
    /// 击毁者等级视为另一方存活步兵机器人的平均经验所对应的等级。平均经验取
    ///     四舍五入后的值。
    /// 机器人因装甲模块被攻击外的其他原因导致变为非存活状态或裁判系统无法检测到
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
        var victimLevel = victim is IExperienced exp ? PerformanceSystem.GetLevel(exp) : 10;
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
                killerLevel = RM2025ulPerformanceSystem.GetLevel(avgExp);
            }
        }

        var levelDiff = victimLevel - killerLevel;
        if (levelDiff < 0)
            levelDiff = 0;

        var expGained = 50 * victimLevel * (1 + 0.2 * levelDiff);
        ExperienceSystem.AddExp(killer, (int)expGained);
    }

    [Route]
    private void OnDamage(DamageCommand damageCommand)
    {
        var shooter = damageCommand.Shooter;
        var victim = damageCommand.Victim;
        var damage = damageCommand.Damage;
        if (TimeSystem.Stage != JudgeSystemStage.Match)
            return;
        if (shooter is not IExperienced exp)
            return;

        if (victim is IRobot)
        {
            ExperienceSystem.AddExp(exp, (int)(damage * 4));
        }
    }
}