using VitalRouter;

namespace RoboSouls.JudgeSystem.RoboMaster2025UL.Events;

public readonly struct WinPointCommand : ICommand
{
    public readonly Camp Camp;
    public readonly int NewWinPoint;

    public WinPointCommand(Camp camp, int newWinPoint)
    {
        Camp = camp;
        NewWinPoint = newWinPoint;
    }
}