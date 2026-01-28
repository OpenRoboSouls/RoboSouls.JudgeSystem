using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RoboSouls.JudgeSystem.Entities;
using RoboSouls.JudgeSystem.Events;
using RoboSouls.JudgeSystem.Systems;
using VContainer;
using VitalRouter;

namespace RoboSouls.JudgeSystem.RoboMaster2026UC.Systems;

/// <summary>
///     积分系统
/// </summary>
[Routes]
public sealed partial class ScoreSystem(ICacheProvider<int> intCacheBox, EntitySystem entitySystem) : IScoreSystem
{
    private static readonly int KillCountCacheKey = "KillCount".GetHashCode();
    private static readonly int DeathCountCacheKey = "DeathCount".GetHashCode();

    public int GetScore(in Identity id)
    {
        if (entitySystem.TryGetEntity(id, out IExperienced exp))
            return exp.Experience;

        return 0;
    }

    public Task Reset(CancellationToken cancellation = new())
    {
        return Task.WhenAll(
            entitySystem.Entities.Values.Select(e =>
            {
                SetKillCount(e.Id, 0);
                SetDeathCount(e.Id, 0);
                return Task.CompletedTask;
            })
        );
    }

    [Inject]
    internal void Inject(Router router)
    {
        MapTo(router);
    }

    public int GetKillCount(Identity id)
    {
        return intCacheBox.WithReaderNamespace(id).Load(KillCountCacheKey);
    }

    private void SetKillCount(Identity id, int count)
    {
        intCacheBox.WithWriterNamespace(id).Save(KillCountCacheKey, count);
    }

    public int GetDeathCount(Identity id)
    {
        return intCacheBox.WithReaderNamespace(id).Load(DeathCountCacheKey);
    }

    private void SetDeathCount(Identity id, int count)
    {
        intCacheBox.WithWriterNamespace(id).Save(DeathCountCacheKey, count);
    }

    [Route]
    private void OnKill(KillEvent evt)
    {
        SetKillCount(evt.Killer, GetKillCount(evt.Killer) + 1);
        SetDeathCount(evt.Victim, GetDeathCount(evt.Victim) + 1);
    }
}