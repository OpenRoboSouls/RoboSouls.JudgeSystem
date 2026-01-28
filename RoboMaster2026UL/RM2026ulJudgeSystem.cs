using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RoboSouls.JudgeSystem.RoboMaster2026UL.Entities;
using RoboSouls.JudgeSystem.RoboMaster2026UL.Systems;
using RoboSouls.JudgeSystem.Systems;
using VContainer;

namespace RoboSouls.JudgeSystem.RoboMaster2026UL;

public sealed class RM2026ulJudgeSystem : JudgeSystem
{
    [Inject] internal ILogger Logger { get; set; }

    [Inject] internal ITimeSystem TimeSystem { get; set; }

    [Inject] internal IEnumerable<ISystem> Systems { get; set; }

    [Inject] internal ExperienceSystem ExperienceSystem { get; set; }

    public static void Build(IContainerBuilder builder)
    {
        builder
            .Register<Hero>(Lifetime.Scoped)
            .WithParameter("id", Identity.BlueHero)
            .AsImplementedInterfaces();
        builder
            .Register<Hero>(Lifetime.Scoped)
            .WithParameter("id", Identity.RedHero)
            .AsImplementedInterfaces();
        builder
            .Register<Infantry>(Lifetime.Scoped)
            .WithParameter("id", Identity.RedInfantry1)
            .AsImplementedInterfaces();
        builder
            .Register<Infantry>(Lifetime.Scoped)
            .WithParameter("id", Identity.BlueInfantry1)
            .AsImplementedInterfaces();
        builder
            .Register<Sentry>(Lifetime.Scoped)
            .WithParameter("id", Identity.RedSentry)
            .AsImplementedInterfaces();
        builder
            .Register<Sentry>(Lifetime.Scoped)
            .WithParameter("id", Identity.BlueSentry)
            .AsImplementedInterfaces();

        builder.Register<ISystem, EntitySystem>(Lifetime.Scoped).AsSelf();
        builder.Register<ISystem, EconomyDispatchSystem>(Lifetime.Scoped).AsSelf();
        builder
            .Register<PerformanceSystemBase, RM2026ulPerformanceSystem>(Lifetime.Scoped)
            .AsSelf();
        builder.Register<ISystem, EconomySystem>(Lifetime.Scoped).AsSelf();
        builder.Register<ISystem, LifeSystem, RM2026ulLifeSystem>(Lifetime.Scoped).AsSelf();
        builder.Register<ISystem, ExperienceSystem>(Lifetime.Scoped).AsSelf();
        builder.Register<ISystem, BuffSystem>(Lifetime.Scoped).AsSelf();
        builder.Register<ISystem, SupplySystem>(Lifetime.Scoped).AsSelf();
        builder.Register<ISystem, OperatorSystem>(Lifetime.Scoped).AsSelf();
        builder.Register<JudgeSystem, RM2026ulJudgeSystem>(Lifetime.Scoped).AsSelf();
        builder
            .Register<ISystem, PowerManagementSystemBase, PowerManagementSystem>(
                Lifetime.Scoped
            )
            .AsSelf();
        builder.Register<ISystem, SentrySystem>(Lifetime.Scoped).AsSelf();

        builder.Register<ZoneSystem>(Lifetime.Scoped).As<ISystem>().AsSelf();
        builder
            .Register<BattleSystem>(Lifetime.Scoped)
            .As<IBattleSystem>()
            .As<ISystem>()
            .AsSelf();
        builder.Register<JudgeBotSystem>(Lifetime.Scoped).As<ISystem>().AsSelf();
        builder.Register<WinPointSystem>(Lifetime.Scoped).As<ISystem>().AsSelf();
        builder
            .Register<ScoreSystem>(Lifetime.Scoped)
            .As<IScoreSystem>()
            .As<ISystem>()
            .AsSelf();
        builder
            .Register<ModuleSystem>(Lifetime.Scoped)
            .As<ModuleSystemBase>()
            .As<ISystem>()
            .AsSelf();
        builder.Register<SettlementSystem>(Lifetime.Scoped).As<ISystem>().AsSelf();
    }

    public override async Task StartAsync(CancellationToken cancellation = new())
    {
        await base.StartAsync(cancellation);

        await Reset(cancellation);

        Logger.Info("Judge system for RoboMaster 2024ul starts.");
        TimeSystem.SetStage(JudgeSystemStage.Repair);
    }

    public override async Task Reset(
        CancellationToken cancellation = new()
    )
    {
        base.Reset(cancellation);

        // reset before other systems
        await TimeSystem.Reset(cancellation);
        await ExperienceSystem.Reset(cancellation);

        var systems = Systems.Except(new ISystem[] { ExperienceSystem, TimeSystem });
        await Task.WhenAll(systems.Select(system => system.Reset(cancellation)));
    }
}