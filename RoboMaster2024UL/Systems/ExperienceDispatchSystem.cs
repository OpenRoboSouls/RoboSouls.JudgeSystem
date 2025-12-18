using System.Linq;
using RoboSouls.JudgeSystem.Entities;
using RoboSouls.JudgeSystem.Events;
using RoboSouls.JudgeSystem.RoboMaster2024UL.Entities;
using RoboSouls.JudgeSystem.RoboMaster2024UL.Events;
using RoboSouls.JudgeSystem.Systems;
using VContainer;
using VitalRouter;

namespace RoboSouls.JudgeSystem.RoboMaster2024UL.Systems;

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
        battleSystem.OnDamage += OnDamage;
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
    internal RM2024ulPerformanceSystem PerformanceSystem { get; set; }

    /// <summary>
    /// 占领中央增益点
    /// "该方存活的英雄和步兵机器人评分500经验值"
    /// </summary>
    /// <param name="evt"></param>
    [Route]
    private void OnCentralZoneOccupied(CentralZoneOccupiedEvent evt)
    {
        var robots = EntitySystem
            .GetOperatedEntities<IExperienced>(evt.Camp)
            .Where(r => r is IRobot)
            .ToList();

        var expPerRobot = 500 / robots.Count;
        foreach (var robot in robots)
        {
            ExperienceSystem.AddExp(robot, expPerRobot);
        }
    }

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
    /// 造成攻击伤害
    ///  对机器人造成攻击伤害：每造成 1 点伤害，攻击方获得 4 点经验
    ///  对基地造成攻击伤害：每造成 1 点伤害，攻击方获得 1 点经验
    /// </summary>
    /// <param name="shooter"></param>
    /// <param name="victim"></param>
    /// <param name="damage"></param>
    private void OnDamage(IShooter shooter, IHealthed victim, uint damage)
    {
        if (TimeSystem.Stage != JudgeSystemStage.Match)
            return;
        if (shooter is not IExperienced exp)
            return;

        if (victim is Base)
        {
            ExperienceSystem.AddExp(exp, (int)damage);
        }
        else if (victim is IRobot)
        {
            ExperienceSystem.AddExp(exp, (int)(damage * 4));
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
                killerLevel = RM2024ulPerformanceSystem.GetLevel(avgExp);
            }
        }

        var levelDiff = victimLevel - killerLevel;
        if (levelDiff < 0)
            levelDiff = 0;

        var expGained = 50 * victimLevel * (1 + 0.2 * levelDiff);
        ExperienceSystem.AddExp(killer, (int)expGained);
    }
}