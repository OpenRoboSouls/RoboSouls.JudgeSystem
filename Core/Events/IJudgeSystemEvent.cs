using System;
using VitalRouter;

namespace RoboSouls.JudgeSystem.Events;

public interface IJudgeSystemEvent : ICommand;

public interface IJudgeSystemEvent<T> : IJudgeSystemEvent
    where T : unmanaged, IEquatable<T>;