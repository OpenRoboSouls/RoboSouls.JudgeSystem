using System;
using System.Threading;
using System.Threading.Tasks;

namespace RoboSouls.JudgeSystem.Systems;

public sealed class EconomySystem(ICacheProvider<int> economyBox, ILogger Logger) : ISystem
{
    private static readonly int RedCoinCacheKey = "RedCoin".Sum();
    private static readonly int BlueCoinCacheKey = "BlueCoin".Sum();

    public int RedCoin => economyBox.Load(RedCoinCacheKey);

    public int RedTotalCoin
    {
        get => economyBox.Load(RedCoinCacheKey + 1);
        private set => economyBox.Save(RedCoinCacheKey + 1, value);
    }

    public int RedCoinCost => RedTotalCoin - RedCoin;

    public int BlueCoin => economyBox.Load(BlueCoinCacheKey);

    public int BlueTotalCoin
    {
        get => economyBox.Load(BlueCoinCacheKey + 1);
        private set => economyBox.Save(BlueCoinCacheKey + 1, value);
    }

    public int BlueCoinCost => BlueTotalCoin - BlueCoin;

    public int GetCoin(Camp camp)
    {
        return camp switch
        {
            Camp.Red => RedCoin,
            Camp.Blue => BlueCoin,
            _ => throw new ArgumentOutOfRangeException(nameof(camp), camp, null),
        };
    }
        
    public void AddCoin(Camp camp, int amount)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), amount, null);
        }
        
        switch (camp)
        {
            case Camp.Red:
                RedTotalCoin += amount;
                economyBox.Save(RedCoinCacheKey, RedCoin + amount);
                break;
            case Camp.Blue:
                BlueTotalCoin += amount;
                economyBox.Save(BlueCoinCacheKey, BlueCoin + amount);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(camp), camp, null);
        }
    }

    public bool TryDecreaseCoin(Camp camp, int amount)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "should not pass negative value");
        }
        
        switch (camp)
        {
            case Camp.Red:
                if (RedCoin < amount)
                {
                    return false;
                }
                economyBox.Save(RedCoinCacheKey, RedCoin - amount);
                break;
            case Camp.Blue:
                if (BlueCoin < amount)
                {
                    return false;
                }
                economyBox.Save(BlueCoinCacheKey, BlueCoin - amount);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(camp), camp, null);
        }

        return true;
    }

    public Task Reset(CancellationToken cancellation = new CancellationToken())
    {
        economyBox.Save(RedCoinCacheKey, 0);
        economyBox.Save(BlueCoinCacheKey, 0);
        return Task.CompletedTask;
    }
}