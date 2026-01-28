using VContainer;

namespace RoboSouls.JudgeSystem.Systems;

public abstract class PowerManagementSystemBase : ISystem
{
    private static readonly int EnergyConsumptionKey = "EnergyConsumption".GetHashCode();

    [Inject] protected ILogger Logger { get; set; }

    [Inject] protected ICacheProvider<float> FloatCacheProvider { get; set; }

    public virtual void OnEnergyConsumption(Identity robot, float consumption)
    {
        var currentConsumption = GetEnergyConsumption(robot);
        SetEnergyConsumption(robot, currentConsumption + consumption);

        // Logger.Info($"Robot {robot} consumed {consumption} energy, total consumption: {currentConsumption + consumption}");
    }

    public virtual float GetEnergyConsumption(Identity robot)
    {
        return FloatCacheProvider.WithReaderNamespace(robot).Load(EnergyConsumptionKey);
    }

    protected virtual void SetEnergyConsumption(Identity robot, float consumption)
    {
        FloatCacheProvider.WithWriterNamespace(robot).Save(EnergyConsumptionKey, consumption);
    }
}