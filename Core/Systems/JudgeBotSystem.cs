using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RoboSouls.JudgeSystem.Entities;
using RoboSouls.JudgeSystem.Events;
using VContainer;
using VitalRouter;

namespace RoboSouls.JudgeSystem.Systems;

/// <summary>
/// 判罚系统
/// </summary>
[Routes]
public partial class JudgeBotSystem : ISystem
{
    [Inject]
    internal void Inject(Router router)
    {
        MapTo(router);
    }

    /// <summary>
    /// 故意冲撞
    ///
    /// 一方机器人不得使用自身任意结构冲撞对方机器人。 若战亡机器人造成关键移动路径的阻挡，可缓慢将其推开
    /// </summary>
    public const byte PenaltyReasonIntentionalCollision = 1;

    /// <summary>
    /// 干扰复活
    ///
    /// 一方机器人不可使用除发射弹丸外的任何手段干扰对方机器人补血或复活。
    /// </summary>
    public const byte PenaltyReasonInterfereRevive = 2;

    /// <summary>
    /// 阻挡补给区、兑换区
    ///
    /// 方机器人及其行为均不可阻挡另一方机器人进入其补给区、兑换区
    /// </summary>
    public const byte PenaltyReasonBlockSupplyExchange = 3;

    /// <summary>
    /// 进入禁区
    /// </summary>
    public const byte PenaltyReasonEnterForbiddenZone = 4;

    /// <summary>
    /// 抢跑
    /// </summary>
    public const byte PenaltyReasonFalseStart = 5;

    /// <summary>
    /// 黄牌累计
    /// </summary>
    public const byte PenaltyReasonMaximumYellowCard = 6;

    public static readonly Identity RedForbiddenZoneId = new Identity(Camp.Red, 255);
    public static readonly Identity BlueForbiddenZoneId = new Identity(Camp.Blue, 255);
    public static readonly Identity BothForbiddenZoneId = new Identity(Camp.Judge, 255);

    private static readonly int YellowCardCountCacheKey = "YellowCardCount".Sum();

    [Inject]
    internal ICommandPublisher Publisher { get; set; }

    [Inject]
    internal LifeSystem LifeSystem { get; set; }

    [Inject]
    internal EntitySystem EntitySystem { get; set; }

    [Inject]
    internal ILogger Logger { get; set; }

    [Inject]
    internal BuffSystem BuffSystem { get; set; }

    [Inject]
    internal ITimeSystem TimeSystem { get; set; }

    [Inject]
    internal ZoneSystem ZoneSystem { get; set; }

    [Inject]
    internal ICacheProvider<int> IntCacheBox { get; set; }

    [Inject]
    internal PerformanceSystemBase PerformanceSystem { get; set; }

    public Task Reset(CancellationToken cancellation = new CancellationToken())
    {
        TimeSystem.RegisterRepeatAction(1, ForbiddenZoneDetectLoop);

        return Task.CompletedTask;
    }

    private void ForbiddenZoneDetectLoop()
    {
        if (TimeSystem.Stage is not JudgeSystemStage.Match)
            return;

        foreach (var robot in EntitySystem.GetOperatedEntities<IHealthed>(Camp.Red))
        {
            if (!robot.IsDead())
            {
                ForbiddenZoneDetectLoopFor(robot.Id);
            }
        }

        foreach (var robot in EntitySystem.GetOperatedEntities<IHealthed>(Camp.Blue))
        {
            if (!robot.IsDead())
            {
                ForbiddenZoneDetectLoopFor(robot.Id);
            }
        }
    }

    private readonly Dictionary<Identity, double> _inForbiddenZoneTime =
        new Dictionary<Identity, double>();
    private const double ForbiddenZoneTimeThreshold = 5.0;

    private void ForbiddenZoneDetectLoopFor(Identity id)
    {
        var fid = id.Camp == Camp.Red ? RedForbiddenZoneId : BlueForbiddenZoneId;

        if (
            !BuffSystem.TryGetBuff(id, Buffs.YellowCard, out Buff _)
            && ZoneSystem.GetInZones(id).Any(z => z == fid || z == BothForbiddenZoneId)
        )
        {
            var t = _inForbiddenZoneTime.GetValueOrDefault(id, TimeSystem.Time);

            if (TimeSystem.Time - t > ForbiddenZoneTimeThreshold)
            {
                _inForbiddenZoneTime.Remove(id);

                if (TimeSystem.Time - t < ForbiddenZoneTimeThreshold * 2)
                {
                    // 进入禁区
                    Logger.Info($"JudgeBotSystem: {id} enter forbidden zone");
                    Penalty(
                        Identity.Server,
                        id,
                        PenaltyType.YellowCard,
                        PenaltyReasonEnterForbiddenZone
                    );
                }
            }
            else
            {
                _inForbiddenZoneTime[id] = t;
            }
        }
    }

    public int GetYellowCardCount(in Identity id)
    {
        return IntCacheBox.WithReaderNamespace(id).Load(YellowCardCountCacheKey);
    }

    private void SetYellowCardCount(in Identity id, int count)
    {
        IntCacheBox.WithWriterNamespace(id).Save(YellowCardCountCacheKey, count);
    }

    [Route]
    private void OnJudgeSystemStageChange(JudgeSystemStageChangedEvent evt)
    {
        // 判罚系统阶段变更
    }

    public void Penalty(
        in Identity judgeId,
        in Identity targetId,
        PenaltyType penaltyType,
        byte reason
    )
    {
        Logger.Info(
            $"Judge {judgeId} penalizes {targetId} with {penaltyType} for reason {reason}"
        );

        if (penaltyType == PenaltyType.RedCard)
        {
            BuffSystem.AddBuff(targetId, Buffs.RedCard, 1, TimeSpan.MaxValue);

            if (EntitySystem.TryGetEntity(targetId, out IHealthed healthed))
            {
                LifeSystem.DecreaseHealth(healthed, judgeId, uint.MaxValue);
            }
        }
        else if (penaltyType == PenaltyType.YellowCard)
        {
            var yellowCardCount = GetYellowCardCount(targetId);
            yellowCardCount = Math.Min(
                yellowCardCount + 1,
                PerformanceSystem.MaxYellowCardCount
            );
            var maxYellowCardCount = PerformanceSystem.MaxYellowCardCount;
            if (targetId.IsEngineer())
            {
                maxYellowCardCount /= 2;
            }
            if (yellowCardCount >= maxYellowCardCount)
            {
                Penalty(judgeId, targetId, PenaltyType.RedCard, PenaltyReasonMaximumYellowCard);
            }
            else
            {
                // BuffSystem.AddBuff(targetId, Buffs.YellowCard);
                if (targetId.IsSentry())
                {
                    //若违规机器人为哨兵机器人，则其底盘断电 2 秒，其它机器人操作界面被
                    // 遮挡 2 秒。
                    BuffSystem.AddBuff(
                        targetId,
                        Buffs.ChassisPowerOffBuff,
                        1,
                        TimeSpan.FromSeconds(2)
                    );
                }
                else
                {
                    // 若违规机器人不为哨兵机器人，则其操作界面被遮挡 5 秒，其它机器人操
                    // 作界面被遮挡 2 秒。
                    BuffSystem.AddBuff(targetId, Buffs.YellowCard, 1, TimeSpan.FromSeconds(5));
                }

                var tid = targetId;
                var otherRobots = EntitySystem
                    .GetOperatedEntities<IRobot>(targetId.Camp)
                    .Where(x => x.Id != tid)
                    .ToArray();
                foreach (var robot in otherRobots)
                {
                    BuffSystem.AddBuff(
                        robot.Id,
                        Buffs.YellowCardTeammate,
                        1,
                        TimeSpan.FromSeconds(2)
                    );
                }
            }

            SetYellowCardCount(targetId, yellowCardCount);
        }

        Publisher.PublishAsync(new JudgePenaltyEvent(penaltyType, targetId, judgeId, reason));
    }
}

public enum PenaltyType
{
    YellowCard,
    RedCard,
    Lose,
}