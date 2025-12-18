using RoboSouls.JudgeSystem.Entities;
using RoboSouls.JudgeSystem.Events;
using VContainer;
using VitalRouter;

namespace RoboSouls.JudgeSystem.Systems;

public class OperatorSystem : ISystem
{
    [Inject]
    public LifeSystem LifeSystem { get; set; }

    [Inject]
    public ILogger Logger { get; set; }

    [Inject]
    public EntitySystem EntitySystem { get; set; }

    [Inject]
    public ITimeSystem TimeSystem { get; set; }

    [Inject]
    public ICommandPublisher CommandPublisher { get; set; }

    public void OperatorLogin(in Identity id)
    {
        if (!EntitySystem.TryGetEntity(id, out IRobot robot))
            return;

        Logger.Info($"Operator of {id} logged in");
        EntitySystem.AssignOperator(id);
        if (robot is IHealthed healthed)
        {
            LifeSystem.ResetHealth(healthed);
        }

        CommandPublisher.PublishAsync(new OperatorLoginEvent(id));
    }

    public void OperatorLogout(in Identity id)
    {
        if (!EntitySystem.TryGetEntity(id, out IRobot _))
            return;

        Logger.Info($"Operator of {id} logged out");
        EntitySystem.RemoveOperator(id);

        CommandPublisher.PublishAsync(new OperatorLogoutEvent(id));
    }
}