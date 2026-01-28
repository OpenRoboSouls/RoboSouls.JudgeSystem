using RoboSouls.JudgeSystem.Systems;
using VContainer;

namespace RoboSouls.JudgeSystem.RoboMaster2025UC.Systems;

public sealed class RM2025ucOperatorSystem : OperatorSystem
{
    private static readonly int ControlModeCacheKey = "control_mode".Sum();

    [Inject] internal ICacheProvider<byte> ByteCacheBox { get; set; }

    public ControlMode GetControlMode(in Identity id)
    {
        var value = ByteCacheBox
            .WithReaderNamespace(id)
            .TryLoad(ControlModeCacheKey, out var mode)
            ? mode
            : (byte)ControlMode.Manual;

        return (ControlMode)value;
    }

    private bool TrySetControlMode(in Identity id, ControlMode mode)
    {
        if (GetControlMode(id) == mode)
            return true;

        if (id.IsEngineer())
        {
            ByteCacheBox.WithWriterNamespace(id).Save(ControlModeCacheKey, (byte)mode);
            return true;
        }

        if (id.IsSentry() || id.IsHero())
        {
            if (mode == ControlMode.AutoExchange)
                return false;
            ByteCacheBox.WithWriterNamespace(id).Save(ControlModeCacheKey, (byte)mode);
            return true;
        }

        return false;
    }
}

public enum ControlMode : byte
{
    /// <summary>
    ///     手动控制
    /// </summary>
    Manual,

    /// <summary>
    ///     半自动控制
    /// </summary>
    SemiAuto,

    /// <summary>
    ///     自动兑矿
    /// </summary>
    AutoExchange
}