namespace RoboSouls.JudgeSystem.Events;

public readonly record struct JudgeSystemStageChangedEvent(JudgeSystemStage Prev, JudgeSystemStage Next)
    : IJudgeSystemEvent<JudgeSystemStageChangedEvent>;