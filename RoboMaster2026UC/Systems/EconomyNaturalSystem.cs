using System.Threading;
using System.Threading.Tasks;
using RoboSouls.JudgeSystem.Systems;

namespace RoboSouls.JudgeSystem.RoboMaster2026UC.Systems;

/// <summary>
///     经济自然增长
///     比赛倒计时 红方 蓝方
///     06:59 400（初始） 400（初始）
///     05:59 50 50
///     04:59 50 50
///     03:59 50 50
///     03:59 50 50
///     01:59 50 50
///     00:59 150 150
/// </summary>
public sealed class EconomyNaturalSystem(
    EconomySystem economySystem,
    ITimeSystem timeSystem,
    ILogger logger,
    IMatchConfigurationRM2026uc matchConfiguration)
    : ISystem
{
    public Task Reset(CancellationToken cancellation = new())
    {
        timeSystem.RegisterOnceAction(
            JudgeSystemStage.Match,
            0,
            () =>
            {
                economySystem.AddCoin(Camp.Red, matchConfiguration.GetInitialCoin(Camp.Red));
                economySystem.AddCoin(Camp.Blue, matchConfiguration.GetInitialCoin(Camp.Blue));
            }
        );
        timeSystem.RegisterOnceAction(JudgeSystemStage.Match, 60, () => BothSideAddMoney(50));
        timeSystem.RegisterOnceAction(JudgeSystemStage.Match, 120, () => BothSideAddMoney(50));
        timeSystem.RegisterOnceAction(JudgeSystemStage.Match, 180, () => BothSideAddMoney(50));
        timeSystem.RegisterOnceAction(JudgeSystemStage.Match, 240, () => BothSideAddMoney(50));
        timeSystem.RegisterOnceAction(JudgeSystemStage.Match, 300, () => BothSideAddMoney(50));
        timeSystem.RegisterOnceAction(JudgeSystemStage.Match, 360, () => BothSideAddMoney(150));

        return Task.CompletedTask;
    }

    private Task BothSideAddMoney(int money)
    {
        logger.Info($"[EconomyNatural] Both side add money: {money}");
        economySystem.AddCoin(Camp.Red, money);
        economySystem.AddCoin(Camp.Blue, money);

        return Task.CompletedTask;
    }
}