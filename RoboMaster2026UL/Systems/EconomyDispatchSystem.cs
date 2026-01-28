using System.Threading;
using System.Threading.Tasks;
using RoboSouls.JudgeSystem.Systems;
using VContainer;

namespace RoboSouls.JudgeSystem.RoboMaster2026UL.Systems;

/// <summary>
///     经济分发
///     5:00 - 200
///     4:00 - 200
///     3:00 - 200
///     2:00 - 300
///     1:00 - 300
/// </summary>
public sealed class EconomyDispatchSystem : ISystem
{
    [Inject] internal EconomySystem EconomySystem { get; set; }

    [Inject] internal ITimeSystem TimeSystem { get; set; }

    [Inject] internal ILogger Logger { get; set; }

    public Task Reset(CancellationToken cancellation = new())
    {
        TimeSystem.RegisterOnceAction(JudgeSystemStage.Match, 0, () => BothSideAddMoney(200));
        TimeSystem.RegisterOnceAction(JudgeSystemStage.Match, 60, () => BothSideAddMoney(200));
        TimeSystem.RegisterOnceAction(JudgeSystemStage.Match, 120, () => BothSideAddMoney(200));
        TimeSystem.RegisterOnceAction(JudgeSystemStage.Match, 180, () => BothSideAddMoney(300));
        TimeSystem.RegisterOnceAction(JudgeSystemStage.Match, 240, () => BothSideAddMoney(300));

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