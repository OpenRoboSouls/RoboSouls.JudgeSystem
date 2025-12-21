using System;
using System.Threading;
using System.Threading.Tasks;

namespace RoboSouls.JudgeSystem.Systems;

public sealed class EconomySystem(ICacheProvider<int> economyBox, ILogger Logger) : ISystem
{
    private static readonly int RedCoinCacheKey = "RedCoin".Sum();
    private static readonly int BlueCoinCacheKey = "BlueCoin".Sum();

    public int RedCoin
    {
        get => economyBox.Load(RedCoinCacheKey);
        set
        {
            if (value > RedCoin)
            {
                RedTotalCoin += value - RedCoin;
            }

            economyBox.Save(RedCoinCacheKey, value);
        }
    }

    public int RedTotalCoin
    {
        get => economyBox.Load(RedCoinCacheKey + 1);
        private set => economyBox.Save(RedCoinCacheKey + 1, value);
    }

    public int BlueCoin
    {
        get => economyBox.Load(BlueCoinCacheKey);
        set
        {
            if (value > BlueCoin)
            {
                BlueTotalCoin += value - BlueCoin;
            }

            economyBox.Save(BlueCoinCacheKey, value);
        }
    }

    public int BlueTotalCoin
    {
        get => economyBox.Load(BlueCoinCacheKey + 1);
        private set => economyBox.Save(BlueCoinCacheKey + 1, value);
    }

    public int GetCoin(Camp camp)
    {
        return camp switch
        {
            Camp.Red => RedCoin,
            Camp.Blue => BlueCoin,
            _ => throw new ArgumentOutOfRangeException(nameof(camp), camp, null),
        };
    }

    public void SetCoin(Camp camp, int value)
    {
        switch (camp)
        {
            case Camp.Red:
                RedCoin = value;
                break;
            case Camp.Blue:
                BlueCoin = value;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(camp), camp, null);
        }
    }
        
    public void AddCoin(Camp camp, int value)
    {
        switch (camp)
        {
            case Camp.Red:
                RedCoin += value;
                break;
            case Camp.Blue:
                BlueCoin += value;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(camp), camp, null);
        }
    }

    public Task Reset(CancellationToken cancellation = new CancellationToken())
    {
        RedCoin = 0;
        BlueCoin = 0;
        return Task.CompletedTask;
    }
}