using System;
using System.Security.Cryptography;

namespace SourceGenerator.Utils;

public static class Hash
{
    public static int HashCode(string str, int seed = 0)
    {
        using var sha = SHA256.Create();
        var h = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(str));
        return BitConverter.ToInt32(h, 0) ^ seed;
    }
}