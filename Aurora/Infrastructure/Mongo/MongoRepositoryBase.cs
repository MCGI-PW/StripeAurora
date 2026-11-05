using MongoDB.Driver;

namespace AuroraPet.Infrastructure.Mongo;

/// <summary>
/// Base dos repositorios MongoDB. Os casos de uso foram escritos no estilo unit of work
/// (carrega, altera a entidade, chama SaveChangesAsync), entao este repositorio guarda
/// as entidades carregadas/incluidas/removidas no escopo da requisicao e grava tudo no
/// SaveChangesAsync - o mesmo contrato que o EF Core oferecia com o Oracle.
/// </summary>
public abstract class MongoRepositoryBase<T> where T : class
{
    private readonly Dictionary<Guid, T> _tracked = new();
    private readonly HashSet<Guid> _added = new();
    private readonly HashSet<Guid> _removed = new();

    protected MongoRepositoryBase(IMongoCollection<T> collection) => Collection = collection;

    protected IMongoCollection<T> Collection { get; }

    /// <summary>Identificador da entidade (mapeado para o campo _id).</summary>
    protected abstract Guid GetId(T entity);

    protected static FilterDefinition<T> ById(Guid id) => Builders<T>.Filter.Eq("_id", id);

    /// <summary>Busca um documento e passa a acompanha-lo, para que alteracoes sejam gravadas no SaveChangesAsync.</summary>
    protected async Task<T?> FindTrackedAsync(FilterDefinition<T> filter, CancellationToken ct)
    {
        var entity = await Collection.Find(filter).FirstOrDefaultAsync(ct);
        if (entity is null) return null;

        var id = GetId(entity);
        if (_removed.Contains(id)) return null;
        if (_tracked.TryGetValue(id, out var existing)) return existing;

        _tracked[id] = entity;
        return entity;
    }

    public Task AddAsync(T entity, CancellationToken ct = default)
    {
        var id = GetId(entity);
        _tracked[id] = entity;
        _added.Add(id);
        _removed.Remove(id);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(T entity, CancellationToken ct = default)
    {
        var id = GetId(entity);
        _tracked.Remove(id);

        // Incluida e removida antes de salvar: nao ha nada a gravar.
        if (!_added.Remove(id))
            _removed.Add(id);

        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        foreach (var (id, entity) in _tracked)
        {
            if (_added.Contains(id))
                await Collection.InsertOneAsync(entity, cancellationToken: ct);
            else
                await Collection.ReplaceOneAsync(ById(id), entity, cancellationToken: ct);
        }

        foreach (var id in _removed)
            await Collection.DeleteOneAsync(ById(id), ct);

        _added.Clear();
        _removed.Clear();
    }
}
