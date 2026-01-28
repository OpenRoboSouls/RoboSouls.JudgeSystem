using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace RoboSouls.JudgeSystem;

public readonly record struct Identity
{
    public const ushort HeroId = 1;
    public const ushort EngineerId = 2;
    public const ushort InfantryId1 = 3;
    public const ushort InfantryId2 = 4;
    public const ushort InfantryId3 = 5;
    public const ushort AerialId = 6;
    public const ushort SentryId = 7;
    public const ushort OutpostId = 8;
    public const ushort BaseId = 9;

    public static readonly Identity RedHero = new(Camp.Red, 1);
    public static readonly Identity RedEngineer = new(Camp.Red, 2);
    public static readonly Identity RedInfantry1 = new(Camp.Red, 3);
    public static readonly Identity RedInfantry2 = new(Camp.Red, 4);
    public static readonly Identity RedInfantry3 = new(Camp.Red, 5);
    public static readonly Identity RedAerial = new(Camp.Red, 6);
    public static readonly Identity RedSentry = new(Camp.Red, 7);
    public static readonly Identity RedOutpost = new(Camp.Red, 8);
    public static readonly Identity RedBase = new(Camp.Red, 9);

    public static readonly Identity BlueHero = new(Camp.Blue, 1);
    public static readonly Identity BlueEngineer = new(Camp.Blue, 2);
    public static readonly Identity BlueInfantry1 = new(Camp.Blue, 3);
    public static readonly Identity BlueInfantry2 = new(Camp.Blue, 4);
    public static readonly Identity BlueInfantry3 = new(Camp.Blue, 5);
    public static readonly Identity BlueAerial = new(Camp.Blue, 6);
    public static readonly Identity BlueSentry = new(Camp.Blue, 7);
    public static readonly Identity BlueOutpost = new(Camp.Blue, 8);
    public static readonly Identity BlueBase = new(Camp.Blue, 9);

    public static readonly Identity Spectator = new(Camp.Spectator, 1);
    public static readonly Identity Server = new(Camp.Judge, 0);
    public static readonly Identity Judge1 = new(Camp.Judge, 1);
    public static readonly Identity Judge2 = new(Camp.Judge, 2);
    public static readonly Identity Judge3 = new(Camp.Judge, 3);
    private readonly byte _data;

    private Identity(byte data)
    {
        _data = data;
    }

    public Identity(Camp camp, ushort id) : this((byte)(((byte)camp << 6) | (id & 0x3F)))
    {
    }

    public Camp Camp => (Camp)(_data >> 6);
    public ushort Id => (ushort)(_data & 0x3F);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override string ToString()
    {
        return IdentityExtensions.ToString(this);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryParse(string str, out Identity identity)
    {
        return IdentityExtensions.TryParse(str, out identity);
    }
}

public static class IdentityExtensions
{
    private static readonly Dictionary<Identity, string> ToStringCache = new();

    private static readonly Dictionary<Identity, string> ShortDescribeCache = new();

    private static readonly Dictionary<Identity, string> DescribeCache = new();

    private static string SToString(Camp camp, ushort id)
    {
        return $"[{camp} {id}]";
    }

    public static string ToString(this Identity identity)
    {
        if (!ToStringCache.TryGetValue(identity, out var result))
        {
            result = SToString(identity.Camp, identity.Id);
            ToStringCache.Add(identity, result);
        }

        return result;
    }

    private static string SShortDescribe(Camp camp, ushort id)
    {
        var builder = new StringBuilder();
        switch (camp)
        {
            case Camp.Red:
                builder.Append("R");
                break;
            case Camp.Blue:
                builder.Append("B");
                break;
            case Camp.Judge:
                builder.Append("J");
                break;
            case Camp.Spectator:
                builder.Append("S");
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        builder.Append(id);
        return builder.ToString();
    }

    public static string ShortDescribe(this Identity identity)
    {
        if (!ShortDescribeCache.TryGetValue(identity, out var result))
        {
            result = SShortDescribe(identity.Camp, identity.Id);
            ShortDescribeCache.Add(identity, result);
        }

        return result;
    }

    public static string SDescribe(Camp camp, ushort id)
    {
        var builder = new StringBuilder();
        switch (camp)
        {
            case Camp.Red:
                builder.Append("R");
                break;
            case Camp.Blue:
                builder.Append("B");
                break;
            case Camp.Judge:
                builder.Append("Judge");
                break;
            case Camp.Spectator:
                builder.Append("Spectator");
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        builder.Append(id);

        switch (id)
        {
            case 1:
                builder.Append(" - Hero");
                break;
            case 2:
                builder.Append(" - Engineer");
                break;
            case 3:
            case 4:
            case 5:
                builder.Append(" - Infantry");
                break;
            case 6:
                builder.Append(" - Aerial");
                break;
            case 7:
                builder.Append(" - Sentry");
                break;
            case 8:
                builder.Append(" - Manipulator");
                break;
        }

        return builder.ToString();
    }

    public static string Describe(this Identity identity)
    {
        if (identity == default) return "None";

        if (!DescribeCache.TryGetValue(identity, out var result))
        {
            result = SDescribe(identity.Camp, identity.Id);
            DescribeCache.Add(identity, result);
        }

        return result;
    }

    public static bool TryParse(string str, out Identity identity)
    {
        identity = default;
        if (str.Length < 2) return false;

        str = str.ToLower();

        var camp = str[0] switch
        {
            'r' => Camp.Red,
            'b' => Camp.Blue,
            'j' => Camp.Judge,
            's' => Camp.Spectator,
            _ => Camp.Spectator
        };

        if (!ushort.TryParse(str[1..], out var id)) return false;

        identity = new Identity(camp, id);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsRobotCamp(in this Identity identity)
    {
        return identity.Camp == Camp.Red || identity.Camp == Camp.Blue;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsHero(in this Identity identity)
    {
        return identity.IsRobotCamp() && identity.Id == 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsEngineer(in this Identity identity)
    {
        return identity.IsRobotCamp() && identity.Id == 2;
    }

    public static bool IsInfantry(in this Identity identity)
    {
        return identity.IsRobotCamp() && identity.Id is >= 3 and <= 5;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAerial(in this Identity identity)
    {
        return identity.IsRobotCamp() && identity.Id == 6;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsSentry(in this Identity identity)
    {
        return identity.IsRobotCamp() && identity.Id == 7;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsManipulator(in this Identity identity)
    {
        return identity.IsRobotCamp() && identity.Id == 8;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsBase(in this Identity identity)
    {
        return identity.IsRobotCamp() && identity.Id == 9;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsOutpost(in this Identity identity)
    {
        return identity.IsRobotCamp() && identity.Id == 8;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Camp GetOppositeCamp(this Camp camp)
    {
        return camp switch
        {
            Camp.Red => Camp.Blue,
            Camp.Blue => Camp.Red,
            _ => Camp.Spectator
        };
    }
}

public enum Camp : byte
{
    Spectator,
    Red,
    Blue,
    Judge
}

[Serializable]
public struct SerializableIdentity : IEquatable<SerializableIdentity>
{
    public Camp camp;
    public ushort id;

    public static implicit operator Identity(SerializableIdentity value)
    {
        return new Identity(value.camp, value.id);
    }

    public static implicit operator SerializableIdentity(Identity value)
    {
        return new SerializableIdentity { camp = value.Camp, id = value.Id };
    }

    public bool Equals(SerializableIdentity other)
    {
        return camp == other.camp && id == other.id;
    }

    public override bool Equals(object obj)
    {
        return obj is SerializableIdentity other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine((int)camp, id);
    }
}