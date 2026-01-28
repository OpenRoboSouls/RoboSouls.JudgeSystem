using System;
using VitalRouter;

namespace RoboSouls.JudgeSystem.Events
{
    public interface IJudgeSystemEvent : ICommand;

    public interface IJudgeSystemEvent<T> : IJudgeSystemEvent, IEquatable<T>
        where T : unmanaged, IEquatable<T>;
}

namespace System.Runtime.CompilerServices
{
    public static class IsExternalInit
    {
    }
}