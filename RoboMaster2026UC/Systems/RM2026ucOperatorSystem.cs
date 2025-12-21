using RoboSouls.JudgeSystem.Systems;

namespace RoboSouls.JudgeSystem.RoboMaster2026UC.Systems;

public sealed class RM2026ucOperatorSystem(ICacheProvider<byte> byteCacheBox) : OperatorSystem
{
    private static readonly int ControlModeCacheKey = "control_mode".Sum();

    public ControlMode GetControlMode(in Identity id)
    {
        var value = byteCacheBox
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
            byteCacheBox.WithWriterNamespace(id).Save(ControlModeCacheKey, (byte)mode);
            return true;
        }
        else if (id.IsSentry() || id.IsHero())
        {
            if (mode == ControlMode.AutoExchange)
                return false;
            byteCacheBox.WithWriterNamespace(id).Save(ControlModeCacheKey, (byte)mode);
            return true;
        }

        return false;
    }
}

public enum ControlMode : byte
{
    /// <summary>
    /// 手动控制
    /// </summary>
    Manual,

    /// <summary>
    /// 半自动控制
    /// </summary>
    SemiAuto,

    /// <summary>
    /// 自动兑矿
    /// </summary>
    AutoExchange,
}