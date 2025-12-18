using System;
using System.Threading;
using System.Threading.Tasks;
using RoboSouls.JudgeSystem.Events;
using RoboSouls.JudgeSystem.RoboMaster2025UC.Entities;
using RoboSouls.JudgeSystem.RoboMaster2025UC.Events;
using RoboSouls.JudgeSystem.Systems;
using VContainer;
using VitalRouter;

namespace RoboSouls.JudgeSystem.RoboMaster2025UC.Systems;

/// <summary>
/// 前哨站机制
///
/// 比赛开始后，中部装甲开始旋转，旋转 5 秒内达到 0.8π rad/s 的速度，
/// 随后保持匀速转动，方向随机。每局比赛中，红蓝双方的前哨站旋转方向保持一致且固定不变。
///
/// 当满足以下任意条件时，一方前哨站装甲停止旋转：
///  该方前哨站被击毁
///  对方基地护甲展开
///  比赛开始 3 分钟后
/// </summary>
[Routes]
public sealed partial class OutpostSystem : ISystem
{
    public const ushort OutpostZoneId = 100;
    public static readonly Identity RedOutpostZoneId = new Identity(Camp.Red, OutpostZoneId);
    public static readonly Identity BlueOutpostZoneId = new Identity(Camp.Blue, OutpostZoneId);
    private static readonly int OutpostZoneDeactivatedCacheKey = "OutpostZoneDeactivated".Sum();

    [Inject]
    internal void Inject(Router router)
    {
        MapTo(router);
    }

    [Inject]
    internal ITimeSystem TimeSystem { get; set; }

    [Inject]
    internal ICacheProvider<bool> BoolCacheBox { get; set; }

    [Inject]
    internal ICacheProvider<float> FloatCacheBox { get; set; }

    [Inject]
    internal IMatchConfiguration MatchConfiguration { get; set; }

    [Inject]
    internal ICommandPublisher CommandPublisher { get; set; }

    [Inject]
    internal EntitySystem EntitySystem { get; set; }

    [Inject]
    internal BuffSystem BuffSystem { get; set; }

    public Task Reset(CancellationToken cancellation = new CancellationToken())
    {
        TimeSystem.RegisterOnceAction(
            JudgeSystemStage.Match,
            0,
            () =>
            {
                var clockwise = MatchConfiguration.Random.Next(2) == 0;

                StartOutpost(Camp.Red, clockwise);
                StartOutpost(Camp.Blue, clockwise);
            }
        );

        TimeSystem.RegisterOnceAction(
            JudgeSystemStage.Match,
            180,
            () =>
            {
                // SetRotating(Camp.Red, false);
                // SetRotating(Camp.Blue, false);
                SetRotateSpeed(Camp.Red, 0);
                SetRotateSpeed(Camp.Blue, 0);
            }
        );

        return Task.CompletedTask;
    }

    private void StartOutpost(Camp camp, bool clockwise)
    {
        SetRotateDirection(camp, clockwise);
        SetRotateSpeed(camp, 0.8f * MathF.PI);
    }

    [Route]
    private void OnKill(KillEvent evt)
    {
        if (evt.Victim.IsOutpost())
        {
            SetRotateSpeed(evt.Victim.Camp, 0);
        }
    }

    [Route]
    private void OnBaseArmorOpen(BaseArmorOpenEvent evt)
    {
        SetRotateSpeed(evt.BaseId.Camp, 0);
    }

    private void SetRotateDirection(Camp camp, bool isClockwise)
    {
        var id = camp switch
        {
            Camp.Red => Identity.RedOutpost,
            Camp.Blue => Identity.BlueOutpost,
            _ => throw new ArgumentOutOfRangeException(),
        };

        BoolCacheBox.WithWriterNamespace(id).Save(Outpost.RotateClockwiseCacheKey, isClockwise);
    }

    private void SetRotateSpeed(Camp camp, float speed)
    {
        if (speed < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(speed));
        }

        var id = camp switch
        {
            Camp.Red => Identity.RedOutpost,
            Camp.Blue => Identity.BlueOutpost,
            _ => throw new ArgumentOutOfRangeException(),
        };

        var outpost = EntitySystem.Entities[id] as Outpost;
        if (outpost.RotateSpeed == speed)
        {
            return;
        }

        FloatCacheBox.WithWriterNamespace(id).Save(Outpost.RotateSpeedCacheKey, speed);

        if (speed > 0)
        {
            CommandPublisher.PublishAsync(
                new OutpostRotateStartEvent(camp, outpost.IsRotateClockwise, speed)
            );
        }
        else
        {
            CommandPublisher.PublishAsync(new OutpostRotateStopEvent(camp));
        }
    }

    [Route]
    private void OnEnterOutpostZone(EnterZoneEvent evt)
    {
        if (TimeSystem.Stage != JudgeSystemStage.Match)
            return;
        if (evt.ZoneId != RedOutpostZoneId && evt.ZoneId != BlueOutpostZoneId)
            return;
        if (evt.OperatorId.Camp != evt.ZoneId.Camp)
            return;
        if (IsOutpostZoneDeactivated(evt.ZoneId.Camp))
            return;

        var cooldownBuffValue = TimeSystem.StageTimeElapsed switch
        {
            >= 120 and < 180 => 2,
            >= 180 and < 300 => 3,
            >= 300 => 5,
            _ => 0,
        };
        if (cooldownBuffValue > 0)
        {
            BuffSystem.AddBuff(
                evt.OperatorId,
                Buffs.CoolDownBuff,
                cooldownBuffValue,
                TimeSpan.MaxValue
            );
        }
    }

    [Route]
    private void OnExitOutpostZone(ExitZoneEvent evt)
    {
        if (evt.ZoneId != RedOutpostZoneId && evt.ZoneId != BlueOutpostZoneId)
            return;
        if (evt.OperatorId.Camp != evt.ZoneId.Camp)
            return;

        if (TimeSystem.Stage == JudgeSystemStage.Match)
        {
            BuffSystem.RemoveBuff(evt.OperatorId, Buffs.CoolDownBuff);
        }
    }

    public bool IsOutpostZoneDeactivated(Camp camp)
    {
        var id = camp == Camp.Red ? RedOutpostZoneId : BlueOutpostZoneId;
        return BoolCacheBox
            .WithReaderNamespace(id)
            .TryLoad(OutpostZoneDeactivatedCacheKey, out var deactivated) && deactivated;
    }

    internal void SetOutpostZoneDeactivated(Camp camp, bool deactivated)
    {
        var id = camp == Camp.Red ? RedOutpostZoneId : BlueOutpostZoneId;
        BoolCacheBox.WithWriterNamespace(id).Save(OutpostZoneDeactivatedCacheKey, deactivated);
    }
}