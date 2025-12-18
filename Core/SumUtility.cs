using System;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace RoboSouls.JudgeSystem;

public static class SumUtility
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Sum(this string s, int seed = 0)
    {
        return SumSha256(s, seed);
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

    private static readonly SHA256 SHA256 = SHA256.Create();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int SumSha256(string s, int seed = 0)
    {
        var h = SHA256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(s));
        return BitConverter.ToInt32(h, 0) + seed;
    }
}