using System;
using RoboSouls.JudgeSystem.Events;

namespace RoboSouls.JudgeSystem.RoboMaster2026UC.Events;

public readonly struct AssemblySuccessEvent(Camp camp, int level, bool isFirst)
    : IJudgeSystemEvent<AssemblySuccessEvent>
{
    public Camp Camp => camp;
    public int Level => level;
    /// <summary>
    /// 是否首次完成
    /// </summary>
    /// <returns></returns>
    public bool IsFirst => isFirst;

    public bool Equals(AssemblySuccessEvent other)
    {
        return Camp == other.Camp && Level == other.Level && IsFirst == other.IsFirst;
    }

    public override bool Equals(object? obj)
    {
        return obj is AssemblySuccessEvent other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine((int)Camp, Level, IsFirst);
    }
}