using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RoboSouls.JudgeSystem.Entities;
using RoboSouls.JudgeSystem.Events;
using VContainer;
using VitalRouter;

namespace RoboSouls.JudgeSystem.Systems;

public sealed class ExperienceSystem : ISystem
{
    [Inject] internal ICacheWriter<int> IntCacheBox { get; set; }

    [Inject] internal EntitySystem EntitySystem { get; set; }

    [Inject] internal ICommandPublisher Publisher { get; set; }

    [Inject] internal PerformanceSystemBase PerformanceSystem { get; set; }

    public Action<IExperienced, int> OnExpChange { get; set; } = delegate { };

    public Task Reset(CancellationToken cancellation = new())
    {
        return Task.WhenAll(
            EntitySystem
                .Entities.Values.OfType<IExperienced>()
                .Select(e =>
                {
                    SetExp(e, 0);
                    return Task.CompletedTask;
                })
        );
    }

    public void AddExp(IExperienced experienced, int exp, bool force = false)
    {
        SetExp(experienced, experienced.Experience + exp);

        if (!force) OnExpChange(experienced, exp);
    }

    private void SetExp(IExperienced experienced, int exp)
    {
        var oldLevel = PerformanceSystem.GetLevel(experienced);
        IntCacheBox.WithWriterNamespace(experienced.Id).Save(IExperienced.ExpCacheKey, exp);
        var newLevel = PerformanceSystem.GetLevel(experienced);

        if (oldLevel != newLevel) Publisher.PublishAsync(new LevelUpdateEvent(experienced.Id, oldLevel, newLevel));
    }
}