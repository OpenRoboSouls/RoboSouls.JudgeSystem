using System.Numerics;

namespace RoboSouls.JudgeSystem.Systems;

public interface ILocationSystem
{
    public bool TryGetEntityLocation(in Identity identity, out Vector2 location);

    public bool TryGetEntityRotation(in Identity identity, out float rotation);
}