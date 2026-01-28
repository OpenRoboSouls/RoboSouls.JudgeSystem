namespace RoboSouls.JudgeSystem;

public enum JudgeSystemStage : byte
{
    // 比赛外
    OutOfMatch,

    // 3分钟检修
    Repair,

    // 裁判系统自检
    SelfCheck,

    // 倒计时
    Countdown,

    // 比赛中
    Match,

    // 结算
    Settlement,

    /// <summary>
    ///     技术暂停
    /// </summary>
    Pause
}

public static class JudgeSystemStageExtensions
{
    public static JudgeSystemStage Next(this JudgeSystemStage stage)
    {
        return stage switch
        {
            JudgeSystemStage.OutOfMatch => JudgeSystemStage.Repair,
            JudgeSystemStage.Repair => JudgeSystemStage.SelfCheck,
            JudgeSystemStage.SelfCheck => JudgeSystemStage.Countdown,
            JudgeSystemStage.Countdown => JudgeSystemStage.Match,
            JudgeSystemStage.Match => JudgeSystemStage.Settlement,
            JudgeSystemStage.Settlement => JudgeSystemStage.OutOfMatch,
            _ => JudgeSystemStage.OutOfMatch
        };
    }
}