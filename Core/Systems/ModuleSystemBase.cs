using System;
using RoboSouls.JudgeSystem.Entities;
using VContainer;

namespace RoboSouls.JudgeSystem.Systems;

/// <summary>
/// 机器人模块管理
/// </summary>
public abstract class ModuleSystemBase : ISystem
{
    [Inject]
    protected ITimeSystem TimeSystem { get; set; }

    [Inject]
    protected EntitySystem EntitySystem { get; set; }

    [Inject]
    protected PerformanceSystemBase PerformanceSystem { get; set; }

    [Inject]
    protected BuffSystem BuffSystem { get; set; }

    public virtual bool IsSettingIncomplete(in Identity id)
    {
        if (
            TimeSystem.Stage
            is JudgeSystemStage.Countdown
            or JudgeSystemStage.Match
            or JudgeSystemStage.Settlement
        )
            return false;
        if (!EntitySystem.HasOperator(id))
            return false;

        return true;
    }

    /// <summary>
    /// 用户设置机器人底盘类型
    /// </summary>
    /// <param name="robotId"></param>
    /// <param name="chassisType"></param>
    /// <returns></returns>
    public abstract bool TrySetRobotChassisType(IChassisd robotId, byte chassisType);

    /// <summary>
    /// 用户设置机器人发射机构类型
    /// </summary>
    /// <param name="robotId"></param>
    /// <param name="gunType"></param>
    /// <returns></returns>
    public abstract bool TrySetRobotGunType(IShooter robotId, byte gunType);

    /// <summary>
    /// 第一视角可视度降低
    /// </summary>
    /// <returns></returns>
    public virtual bool IsFpvVisibilityReduced(in Identity id)
    {
        // // A. 若 Q1 > Q0，该机器人对应操作手电脑的第一视角可视度降低。 直到 Q1 ≤ Q0， 第一视角才会恢复正
        // if (EntitySystem.TryGetEntity(id, out IShooter shooter) &&
        //     PerformanceSystem.GetMaxHeat(shooter) < shooter.Heat)
        // {
        //     return true;
        // }
        //
        // return false;
        return BuffSystem.TryGetBuff(id, Buffs.FpvVisibilityReduced, out float _);
    }

    public virtual void SetFpvVisibilityReduced(in Identity id, bool value)
    {
        if (value)
        {
            BuffSystem.AddBuff(id, Buffs.FpvVisibilityReduced, 1, TimeSpan.MaxValue);
        }
        else
        {
            BuffSystem.RemoveBuff(id, Buffs.FpvVisibilityReduced);
        }
    }

    /// <summary>
    /// 发射机构锁定
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public virtual bool IsGunLocked(in Identity id)
    {
        return BuffSystem.TryGetBuff(id, Buffs.GunLocked, out Buff _);
    }

    public virtual void SetGunLocked(in Identity id, bool value)
    {
        if (!EntitySystem.TryGetOperatedEntity(id, out IShooter _))
            return;

        if (value)
        {
            BuffSystem.AddBuff(id, Buffs.GunLocked, 1, TimeSpan.MaxValue);
        }
        else
        {
            BuffSystem.RemoveBuff(id, Buffs.GunLocked);
        }
    }

    /// <summary>
    /// 裁判系功率倍率
    ///
    /// 1. 从性能体系计算功率倍率
    /// 2. 从buff获得额外倍率
    /// 3. 断电控制
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public virtual float GetChassisPowerMultiplier(in Identity id)
    {
        if (EntitySystem.TryGetOperatedEntity(id, out IHealthed healthed) && healthed.IsDead())
            return 0;

        if (BuffSystem.TryGetBuff(id, Buffs.ChassisPowerOffBuff, out Buff _))
        {
            return 0;
        }

        if (!BuffSystem.TryGetBuff(id, Buffs.PowerBuff, out float buffValue))
        {
            buffValue = 1;
        }

        buffValue = MathF.Sqrt(buffValue);

        if (!EntitySystem.TryGetOperatedEntity(id, out IChassisd chassisd))
            return buffValue;
        var basePower = PerformanceSystem.GetBasePower(chassisd);
        var maxPower = PerformanceSystem.GetMaxPower(chassisd);

        return MathF.Min((float)maxPower / basePower * buffValue, 3);
    }

    public virtual bool IsGimbalPowerOff(in Identity id)
    {
        if (EntitySystem.TryGetOperatedEntity(id, out IHealthed healthed) && healthed.IsDead())
            return true;

        if (BuffSystem.TryGetBuff(id, Buffs.GimbalPowerOffBuff, out Buff _))
            return true;

        return false;
    }

    public virtual bool IsFpvLocked(in Identity id)
    {
        if (TimeSystem.Stage != JudgeSystemStage.Match)
            return false;

        if (BuffSystem.TryGetBuff(id, Buffs.YellowCard, out Buff _))
        {
            return true;
        }

        if (BuffSystem.TryGetBuff(id, Buffs.YellowCardTeammate, out Buff _))
        {
            return true;
        }

        return false;
    }
}