using System;
using VContainer;

namespace RoboSouls.JudgeSystem.Systems;

public interface IBuffSystem
{
    void AddBuff(
        Identity buffable,
        int buffType,
        float buffValue,
        TimeSpan duration,
        bool force = false
    );
    void RemoveBuff(in Identity buffable, int buffType);
    bool TryGetBuff(in Identity buffable, int buffType, out Buff buff);
}

public sealed class BuffSystem : IBuffSystem, ISystem
{
    public static readonly int BuffCacheKey = "buff".Sum();

    [Inject]
    internal ICacheProvider<Buff> BuffCacheBox { get; set; }

    [Inject]
    internal ITimeSystem TimeSystem { get; set; }

    public void AddBuff(
        Identity buffable,
        int buffType,
        float buffValue,
        TimeSpan duration,
        bool force = false
    )
    {
        if (!force)
        {
            if (TryGetBuff(buffable, buffType, out Buff oldBuff))
            {
                if (oldBuff.Value > buffValue || oldBuff.Duration > TimeSpan.MaxValue / 2)
                {
                    return;
                }

                duration += TimeSpan.FromSeconds(oldBuff.EndTime - TimeSystem.Time);
            }
        }

        BuffCacheBox
            .WithWriterNamespace(buffable)
            .WithWriterNamespace(BuffCacheKey)
            .Save(buffType, new Buff(buffType, buffValue, TimeSystem.Time, duration));
    }

    public void RemoveBuff(in Identity buffable, int buffType)
    {
        BuffCacheBox
            .WithWriterNamespace(buffable)
            .WithWriterNamespace(BuffCacheKey)
            .Delete(buffType);
    }

    public bool TryGetBuff(in Identity buffable, int buffType, out Buff buffValue)
    {
        if (
            BuffCacheBox
            .WithReaderNamespace(buffable)
            .WithReaderNamespace(BuffCacheKey)
            .TryLoad(buffType, out var buff)
        )
        {
            if (buff.EndTime >= TimeSystem.Time)
            {
                buffValue = buff;
                return true;
            }

            RemoveBuff(buffable, buffType);
        }

        buffValue = default;
        return false;
    }

    public bool TryGetBuff(in Identity buffable, int buffType, out float buffValue)
    {
        if (TryGetBuff(buffable, buffType, out Buff buff))
        {
            buffValue = buff.Value;
            return true;
        }

        buffValue = 0;
        return false;
    }
}

public readonly struct Buff(int type, float value, double startTime, TimeSpan duration)
    : IEquatable<Buff>
{
    public readonly int Type = type;
    public readonly float Value = value;
    public readonly double StartTime = startTime;
    public readonly TimeSpan Duration = duration;

    public double EndTime => StartTime + Duration.TotalSeconds;

    public bool IsInfinite => Duration > TimeSpan.MaxValue / 2;

    public bool Equals(Buff other)
    {
        return Type == other.Type
               && Value.Equals(other.Value)
               && StartTime.Equals(other.StartTime)
               && Duration.Equals(other.Duration);
    }

    public override bool Equals(object obj)
    {
        return obj is Buff other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Type, Value, StartTime, Duration);
    }
}