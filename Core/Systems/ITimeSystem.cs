using System;
using System.Threading.Tasks;

namespace RoboSouls.JudgeSystem.Systems;

public interface ITimeSystem : ISystem
{
    public double Time { get; }
    public JudgeSystemStage Stage { get; }
    public int StageTimeLimit { get; }
    public double StageTimeElapsed { get; }

    public double StageTimeLeft =>
        Stage == JudgeSystemStage.OutOfMatch
            ? float.MaxValue
            : StageTimeLimit - StageTimeElapsed;

    public void SetStage(JudgeSystemStage stage);
    public void RegisterRepeatAction(double interval, Func<Task> action);
    public void RegisterOnceAction(double delay, Func<Task> action);
    public void RegisterOnceAction(
        JudgeSystemStage stage,
        double stageTime,
        Func<Task> action
    );
}

public static class TimeSystemExtension
{
    public static string FormattedClockTime(this double time)
    {
        if (time < 0)
        {
            return "00:00";
        }

        if (time >= 6000)
        {
            return "--:--";
        }

        var minutes = Math.Floor(time / 60);
        var seconds = Math.Floor(time % 60);

        return $"{minutes:00}:{seconds:00}";
    }

    public static void RegisterRepeatAction(
        this ITimeSystem timeSystem,
        double interval,
        Action action
    )
    {
        timeSystem.RegisterRepeatAction(
            interval,
            () =>
            {
                action();
                return Task.CompletedTask;
            }
        );
    }

    public static void RegisterOnceAction(
        this ITimeSystem timeSystem,
        double delay,
        Action action
    )
    {
        timeSystem.RegisterOnceAction(
            delay,
            () =>
            {
                action();
                return Task.CompletedTask;
            }
        );
    }

    public static void RegisterOnceAction(
        this ITimeSystem timeSystem,
        JudgeSystemStage stage,
        double stageTime,
        Action action
    )
    {
        timeSystem.RegisterOnceAction(
            stage,
            stageTime,
            () =>
            {
                action();
                return Task.CompletedTask;
            }
        );
    }
}