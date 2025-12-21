using System.Collections.Generic;
using System.Linq;
using RoboSouls.JudgeSystem.Events;
using VContainer;
using VitalRouter;

namespace RoboSouls.JudgeSystem.Systems;

/// <summary>
/// translate zone event to buff handle
///
/// local only
/// </summary>
[Routes]
public sealed partial class ZoneSystem : ISystem
{
    [Inject]
    internal void Inject(Router router)
    {
        MapTo(router);
    }

    private readonly Dictionary<Identity, HashSet<Identity>> _entityInZones = new();

    public HashSet<Identity> GetInZones(in Identity id)
    {
        return _entityInZones.TryGetValue(id, out var zones) ? zones : new HashSet<Identity>();
    }

    [Route]
    private void OnEnterZone(EnterZoneEvent evt)
    {
        if (IsInZone(evt.OperatorId, evt.ZoneId))
        {
            return;
        }
        if (!_entityInZones.TryGetValue(evt.OperatorId, out var zones))
        {
            zones = new HashSet<Identity>();
            _entityInZones.Add(evt.OperatorId, zones);
        }

        zones.Add(evt.ZoneId);
    }

    [Route]
    private void OnExitZone(ExitZoneEvent evt)
    {
        if (_entityInZones.TryGetValue(evt.OperatorId, out var zones))
        {
            zones.Remove(evt.ZoneId);
        }
    }

    public bool IsInZone(in Identity entity, in Identity zoneId)
    {
        return _entityInZones.TryGetValue(entity, out var zones) && zones.Contains(zoneId);
    }

    public bool IsInAnyZone(in Identity entity)
    {
        return _entityInZones.TryGetValue(entity, out var zones) && zones.Any(z => z.Id < 200);
    }
}