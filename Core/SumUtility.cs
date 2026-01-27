using System;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace RoboSouls.JudgeSystem;

public static class SumUtility
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Sum(this string s, uint seed = 0)
    {
        return HashFNV1a(s);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int SumSimple(string s, int seed = 0)
    {
        int sum = seed;
        foreach (var c in s)
        {
            sum += c;
        }

        return sum;
    }

    private static readonly SHA256 Sha256 = SHA256.Create();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int SumSha256(string s, int seed = 0)
    {
        var h = Sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(s));
        return BitConverter.ToInt32(h, 0) + seed;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int HashFNV1a(string str) {
        uint hash = 2166136261; // FNV_offset_basis
        foreach (char c in str) {
            hash ^= (byte)c;    // 异或
            hash *= 16777619;   // FNV_prime
        }
        return (int)hash;
    }
}