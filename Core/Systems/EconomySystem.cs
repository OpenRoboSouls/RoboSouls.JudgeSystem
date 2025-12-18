using System;
using System.Threading;
using System.Threading.Tasks;
using VContainer;

namespace RoboSouls.JudgeSystem.Systems;

public sealed class EconomySystem : ISystem
{
    private static readonly int RedCoinCacheKey = "RedCoin".Sum();
    private static readonly int BlueCoinCacheKey = "BlueCoin".Sum();

    [Inject]
    internal ICacheProvider<int> EconomyBox { get; set; }

    [Inject]
    internal ILogger Logger { get; set; }

    public int RedCoin
    {
        get => EconomyBox.Load(RedCoinCacheKey);
        set
        {
            if (value > RedCoin)
            {
                RedTotalCoin += value - RedCoin;
            }

            EconomyBox.Save(RedCoinCacheKey, value);
        }
    }

    public int RedTotalCoin
    {
        get => EconomyBox.Load(RedCoinCacheKey + 1);
        private set => EconomyBox.Save(RedCoinCacheKey + 1, value);
    }

    public int BlueCoin
    {
        get => EconomyBox.Load(BlueCoinCacheKey);
        set
        {
            if (value > BlueCoin)
            {
                BlueTotalCoin += value - BlueCoin;
            }

            EconomyBox.Save(BlueCoinCacheKey, value);
        }
    }

    public int BlueTotalCoin
    {
        get => EconomyBox.Load(BlueCoinCacheKey + 1);
        private set => EconomyBox.Save(BlueCoinCacheKey + 1, value);
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