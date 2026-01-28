using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RoboSouls.JudgeSystem.Events;
using VitalRouter;

namespace RoboSouls.JudgeSystem.Systems;

public abstract class OccupyZoneSystemBase : ISystem
{
    private readonly HashSet<Identity> _inZoneOperators = new();
    public abstract Identity ZoneId { get; }

    public Camp CurrentOccupier { get; private set; } = Camp.Spectator;

    public virtual Task Reset(CancellationToken cancellation = new())
    {
        CurrentOccupier = Camp.Spectator;
        _inZoneOperators.Clear();
        return Task.CompletedTask;
    }

    protected abstract void OnZoneOccupied(Camp camp);
    protected abstract void OnZoneLost(Camp camp);
    protected abstract void OnOccupierEnterZone(in Identity operatorId);
    protected abstract void OnOccupierLeaveZone(in Identity operatorId);

    [Route]
    protected void OnEnterZoneInternal(EnterZoneEvent evt)
    {
        if (evt.ZoneId != ZoneId)
            return;

        _inZoneOperators.Add(evt.OperatorId);

        if (CurrentOccupier == Camp.Spectator)
        {
            CurrentOccupier = evt.OperatorId.Camp;
            OnZoneOccupied(evt.OperatorId.Camp);
        }

        if (CurrentOccupier == evt.OperatorId.Camp) OnOccupierEnterZone(evt.OperatorId);
    }

    [Route]
    protected void OnLeaveZoneInternal(ExitZoneEvent evt)
    {
        if (evt.ZoneId != ZoneId)
            return;

        OnLeaveZone(evt.OperatorId);
    }

    [Route]
    protected virtual void OnKill(KillEvent evt)
    {
        OnLeaveZone(evt.Victim);
    }

    private void OnLeaveZone(Identity id)
    {
        _inZoneOperators.Remove(id);

        if (CurrentOccupier == id.Camp)
        {
            OnOccupierLeaveZone(id);

            if (_inZoneOperators.All(i => i.Camp != id.Camp))
            {
                CurrentOccupier = Camp.Spectator;
                OnZoneLost(id.Camp);

                if (_inZoneOperators.Count > 0)
                {
                    CurrentOccupier = _inZoneOperators.First().Camp;
                    OnZoneOccupied(CurrentOccupier);

                    foreach (var operatorId in _inZoneOperators) OnOccupierEnterZone(operatorId);
                }
            }
        }
    }
}