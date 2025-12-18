using System.Threading;
using System.Threading.Tasks;
using RoboSouls.JudgeSystem.RoboMaster2025UL.Entities;
using RoboSouls.JudgeSystem.Systems;
using VContainer;

namespace RoboSouls.JudgeSystem.RoboMaster2025UL.Systems;

public sealed class SentrySystem : ISystem
{
    [Inject]
    internal ITimeSystem TimeSystem { get; set; }

    [Inject]
    internal BattleSystem BattleSystem { get; set; }

    [Inject]
    internal EntitySystem EntitySystem { get; set; }

    public Task Reset(CancellationToken cancellation = new CancellationToken())
    {
        TimeSystem.RegisterOnceAction(
            JudgeSystemStage.Match,
            0,
            () =>
            {
                if (EntitySystem.TryGetOperatedEntity(Identity.RedSentry, out Sentry rs))
                {
                    BattleSystem.SetAmmoAllowance(rs, 750);
                }

                if (EntitySystem.TryGetOperatedEntity(Identity.BlueSentry, out Sentry bs))
                {
                    BattleSystem.SetAmmoAllowance(bs, 750);
                }
            }
        );

        return Task.CompletedTask;
    }
}