using System;
using System.Threading;
using System.Threading.Tasks;
using RoboSouls.JudgeSystem.Systems;
using VContainer;

namespace RoboSouls.JudgeSystem.RoboMaster2025UC.Systems;

/// <summary>
///     工程机器人机制
///     在比赛的前 3 分钟内，工程机器人拥有 50%防御增益。
/// </summary>
public sealed class EngineerSystem : ISystem
{
    [Inject] internal ITimeSystem TimeSystem { get; set; }

    [Inject] internal BuffSystem BuffSystem { get; set; }

    public Task Reset(CancellationToken cancellation = new())
    {
        TimeSystem.RegisterOnceAction(
            JudgeSystemStage.Match,
            0,
            () =>
            {
                BuffSystem.AddBuff(
                    Identity.RedEngineer,
                    Buffs.DefenceBuff,
                    0.5f,
                    TimeSpan.FromMinutes(3)
                );
                BuffSystem.AddBuff(
                    Identity.BlueEngineer,
                    Buffs.DefenceBuff,
                    0.5f,
                    TimeSpan.FromMinutes(3)
                );
            }
        );

        return Task.CompletedTask;
    }
}