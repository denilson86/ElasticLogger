namespace ElasticLogger.Interfaces
{
    public interface IRepository<TEntity> : IDisposable
        where TEntity : Entity
    {
        Task<TEntity> GetById(Guid id, CancellationToken token);

        Task<TEntity> GetByIdWithEvents(Guid id, CancellationToken token);

        [Obsolete("Usar 'GetById'")]
        Task<TEntity> GetByIdWithoutEvents(Guid id, CancellationToken token);

        Task Save(TEntity entity, CancellationToken token);

        Task Delete(TEntity entity, CancellationToken token);
    }
}
