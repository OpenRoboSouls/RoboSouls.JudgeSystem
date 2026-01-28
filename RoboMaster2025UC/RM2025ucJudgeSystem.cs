using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RoboSouls.JudgeSystem.RoboMaster2025UC.Entities;
using RoboSouls.JudgeSystem.RoboMaster2025UC.Systems;
using RoboSouls.JudgeSystem.Systems;
using VContainer;

namespace RoboSouls.JudgeSystem.RoboMaster2025UC;

public sealed class RM2025ucJudgeSystem : JudgeSystem
{
    [Inject] internal ILogger Logger { get; set; }

    [Inject] internal ITimeSystem TimeSystem { get; set; }

    [Inject] internal IEnumerable<ISystem> Systems { get; set; }

    [Inject] internal ExperienceSystem ExperienceSystem { get; set; }

    public static void Build(IContainerBuilder builder)
    {
        builder
            .Register<Hero>(Lifetime.Scoped)
            .WithParameter("id", Identity.RedHero)
            .AsImplementedInterfaces();
        builder
            .Register<Hero>(Lifetime.Scoped)
            .WithParameter("id", Identity.BlueHero)
            .AsImplementedInterfaces();
        builder
            .Register<Engineer>(Lifetime.Scoped)
            .WithParameter("id", Identity.RedEngineer)
            .AsImplementedInterfaces();
        builder
            .Register<Engineer>(Lifetime.Scoped)
            .WithParameter("id", Identity.BlueEngineer)
            .AsImplementedInterfaces();
        builder
            .Register<Infantry>(Lifetime.Scoped)
            .WithParameter("id", Identity.RedInfantry1)
            .AsImplementedInterfaces();
        builder
            .Register<Infantry>(Lifetime.Scoped)
            .WithParameter("id", Identity.RedInfantry2)
            .AsImplementedInterfaces();
        builder
            .Register<Infantry>(Lifetime.Scoped)
            .WithParameter("id", Identity.BlueInfantry1)
            .AsImplementedInterfaces();
        builder
            .Register<Infantry>(Lifetime.Scoped)
            .WithParameter("id", Identity.BlueInfantry2)
            .AsImplementedInterfaces();
        builder
            .Register<Sentry>(Lifetime.Scoped)
            .WithParameter("id", Identity.RedSentry)
            .AsImplementedInterfaces();
        builder
            .Register<Sentry>(Lifetime.Scoped)
            .WithParameter("id", Identity.BlueSentry)
            .AsImplementedInterfaces();
        builder
            .Register<Aerial>(Lifetime.Scoped)
            .WithParameter("id", Identity.RedAerial)
            .AsImplementedInterfaces();
        builder
            .Register<Aerial>(Lifetime.Scoped)
            .WithParameter("id", Identity.BlueAerial)
            .AsImplementedInterfaces();
        builder
            .Register<Base>(Lifetime.Scoped)
            .WithParameter("id", Identity.RedBase)
            .AsImplementedInterfaces();
        builder
            .Register<Base>(Lifetime.Scoped)
            .WithParameter("id", Identity.BlueBase)
            .AsImplementedInterfaces();
        builder
            .Register<Outpost>(Lifetime.Scoped)
            .WithParameter("id", Identity.RedOutpost)
            .AsImplementedInterfaces();
        builder
            .Register<Outpost>(Lifetime.Scoped)
            .WithParameter("id", Identity.BlueOutpost)
            .AsImplementedInterfaces();

        builder.Register<ISystem, EntitySystem>(Lifetime.Scoped).AsSelf();
        builder.Register<ISystem, EconomyNaturalSystem>(Lifetime.Scoped).AsSelf();
        builder
            .Register<PerformanceSystemBase, RM2025ucPerformanceSystem>(Lifetime.Scoped)
            .AsSelf();
        builder.Register<ISystem, EconomySystem>(Lifetime.Scoped).AsSelf();
        builder.Register<ISystem, LifeSystem, RM2025ucLifeSystem>(Lifetime.Scoped).AsSelf();
        builder.Register<ISystem, ExperienceSystem>(Lifetime.Scoped).AsSelf();
        builder.Register<ISystem, BuffSystem>(Lifetime.Scoped).AsSelf();
        builder.Register<ISystem, SupplySystem>(Lifetime.Scoped).AsSelf();
        builder
            .Register<ISystem, OperatorSystem, RM2025ucOperatorSystem>(Lifetime.Scoped)
            .AsSelf();
        builder.Register<ISystem, AerialSystem>(Lifetime.Scoped).AsSelf();
        builder.Register<ISystem, EngineerSystem>(Lifetime.Scoped).AsSelf();
        builder.Register<ISystem, PowerRuneSystem>(Lifetime.Scoped).AsSelf();
        builder.Register<ISystem, RadarSystem>(Lifetime.Scoped).AsSelf();
        builder
            .Register<ISystem, PowerManagementSystemBase, PowerManagementSystem>(
                Lifetime.Scoped
            )
            .AsSelf();
        builder.Register<JudgeSystem, RM2025ucJudgeSystem>(Lifetime.Scoped).AsSelf();

        builder.Register<JudgeBotSystem>(Lifetime.Scoped).As<ISystem>().AsSelf();
        builder.Register<ZoneSystem>(Lifetime.Scoped).As<ISystem>().AsSelf();
        builder
            .Register<BattleSystem>(Lifetime.Scoped)
            .As<IBattleSystem>()
            .As<ISystem>()
            .AsSelf();
        builder.Register<BaseSystem>(Lifetime.Scoped).As<ISystem>().AsSelf();
        builder.Register<ExperienceDispatchSystem>(Lifetime.Scoped).As<ISystem>().AsSelf();
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
        builder.Register<CentralHighlandSystem>(Lifetime.Scoped).As<ISystem>().AsSelf();
        builder.Register<DartSystem>(Lifetime.Scoped).As<ISystem>().AsSelf();
        builder.Register<ExchangerSystem>(Lifetime.Scoped).As<ISystem>().AsSelf();
        builder.Register<FortressSystem>(Lifetime.Scoped).As<ISystem>().AsSelf();
        builder.Register<HeroSystem>(Lifetime.Scoped).As<ISystem>().AsSelf();
        builder.Register<HighlandTerrainLeapZoneSystem>(Lifetime.Scoped).As<ISystem>().AsSelf();
        builder.Register<LadderHighlandSystem>(Lifetime.Scoped).As<ISystem>().AsSelf();
        builder.Register<OutpostSystem>(Lifetime.Scoped).As<ISystem>().AsSelf();
        builder
            .Register<OverSlopeTerrainLeapZoneSystem>(Lifetime.Scoped)
            .As<ISystem>()
            .AsSelf();
        builder.Register<RoadTerrainLeapZoneSystem>(Lifetime.Scoped).As<ISystem>().AsSelf();
        builder.Register<SentrySystem>(Lifetime.Scoped).As<ISystem>().AsSelf();
        builder.Register<PenaltyDamageSystem>(Lifetime.Scoped).As<ISystem>().AsSelf();
    }

    public override async Task StartAsync(CancellationToken cancellation = new())
    {
        await base.StartAsync(cancellation);

        await Reset(cancellation);

        Logger.Info("Judge system for RoboMaster 2025uc starts.");
        TimeSystem.SetStage(JudgeSystemStage.Repair);
    }

    public override async Task Reset(
        CancellationToken cancellation = new()
    )
    {
        await base.Reset(cancellation);

        // reset before other systems
        await TimeSystem.Reset(cancellation);
        await ExperienceSystem.Reset(cancellation);

        var systems = Systems.Except(new ISystem[] { ExperienceSystem, TimeSystem });
        await Task.WhenAll(systems.Select(system => system.Reset(cancellation)));
    }
}