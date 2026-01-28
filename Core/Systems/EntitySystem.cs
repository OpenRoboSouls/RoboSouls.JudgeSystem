using System.Collections.Generic;
using System.Linq;
using RoboSouls.JudgeSystem.Entities;
using VContainer;

namespace RoboSouls.JudgeSystem.Systems;

public sealed class EntitySystem : ISystem
{
    private static readonly int OperatorCacheKey = "has_operator".Sum();
    private readonly Dictionary<Identity, IEntity> _entities;

    public EntitySystem(IEnumerable<IEntity> entities)
    {
        _entities = entities.ToDictionary(e => e.Id);
    }

    public IReadOnlyDictionary<Identity, IEntity> Entities => _entities;

    [Inject] internal ICacheWriter<byte> OperatorCacheBoxWriter { get; set; }

    [Inject] internal ICacheReader<byte> OperatorCacheBox { get; set; }

    public bool HasOperator(in Identity id)
    {
        return OperatorCacheBox.WithReaderNamespace(id).Exists(OperatorCacheKey);
    }

    public bool HasOperator(IEntity entity)
    {
        return HasOperator(entity.Id);
    }

    public bool TryGetEntity<T>(in Identity id, out T entity)
        where T : IEntity
    {
        if (_entities.TryGetValue(id, out var r) && r is T t)
        {
            entity = t;
            return true;
        }

        entity = default;
        return false;
    }

    public bool TryGetOperatedEntity<T>(in Identity id, out T entity)
        where T : IEntity
    {
        return TryGetEntity(id, out entity) && HasOperator(id);
    }

    public T[] GetOperatedEntities<T>(Camp camp)
        where T : IEntity
    {
        return _entities
            .Values.OfType<T>()
            .Where(e => HasOperator(e.Id) && e.Id.Camp == camp)
            .ToArray();
    }

    public void AssignOperator(in Identity id)
    {
        if (!_entities.TryGetValue(id, out var entity) || entity is not IRobot robot) return;

        OperatorCacheBoxWriter.WithWriterNamespace(id).Save(OperatorCacheKey, 1);
    }

    public void RemoveOperator(in Identity id)
    {
        if (!_entities.TryGetValue(id, out var entity) || entity is not IRobot robot) return;

        OperatorCacheBoxWriter.WithWriterNamespace(id).Delete(OperatorCacheKey);
    }
}