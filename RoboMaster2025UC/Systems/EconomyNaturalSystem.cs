using System.Threading;
using System.Threading.Tasks;
using RoboSouls.JudgeSystem.Systems;
using VContainer;

namespace RoboSouls.JudgeSystem.RoboMaster2025UC.Systems;

/// <summary>
/// 经济自然增长
///
/// 比赛倒计时 红方 蓝方
/// 06:59 400（初始） 400（初始）
/// 05:59 50 50
/// 04:59 50 50
/// 03:59 50 50
/// 03:59 50 50
/// 01:59 50 50
/// 00:59 150 150
/// </summary>
public sealed class EconomyNaturalSystem : ISystem
{
    [Inject]
    internal EconomySystem EconomySystem { get; set; }

    [Inject]
    internal ITimeSystem TimeSystem { get; set; }

    [Inject]
    internal ILogger Logger { get; set; }

    [Inject]
    internal IMatchConfigurationRM2025uc MatchConfiguration { get; set; }

    public Task Reset(CancellationToken cancellation = new CancellationToken())
    {
        TimeSystem.RegisterOnceAction(
            JudgeSystemStage.Match,
            0,
            () =>
            {
                EconomySystem.AddCoin(Camp.Red, MatchConfiguration.GetInitialCoin(Camp.Red));
                EconomySystem.AddCoin(Camp.Blue, MatchConfiguration.GetInitialCoin(Camp.Blue));
            }
        );
        TimeSystem.RegisterOnceAction(JudgeSystemStage.Match, 60, () => BothSideAddMoney(50));
        TimeSystem.RegisterOnceAction(JudgeSystemStage.Match, 120, () => BothSideAddMoney(50));
        TimeSystem.RegisterOnceAction(JudgeSystemStage.Match, 180, () => BothSideAddMoney(50));
        TimeSystem.RegisterOnceAction(JudgeSystemStage.Match, 240, () => BothSideAddMoney(50));
        TimeSystem.RegisterOnceAction(JudgeSystemStage.Match, 300, () => BothSideAddMoney(50));
        TimeSystem.RegisterOnceAction(JudgeSystemStage.Match, 360, () => BothSideAddMoney(150));

        return Task.CompletedTask;
    }

    private Task BothSideAddMoney(int money)
    {
        Logger.Info($"[EconomyNatural] Both side add money: {money}");
        EconomySystem.AddCoin(Camp.Red, money);
        EconomySystem.AddCoin(Camp.Blue, money);

        return Task.CompletedTask;
    }
}