using Nest;

namespace ElasticLogger
{
    public abstract class ElasticsearchRepository<TEntity>
        : Interfaces.IRepository<TEntity>
        where TEntity : Entity
    {
        protected static readonly IEnumerable<TEntity> Empty = new TEntity[0];
        protected static bool IndexCreated;
        protected readonly ElasticClient Client;
        protected readonly IndexName IndexName;
        protected readonly string IndexSearch;

        /// <summary>
        ///     Abstração de repositório para o elasticsearch
        /// </summary>
        protected ElasticsearchRepository(ElasticClient client, string index)
        {
            IndexName = $"{index}.{DateTime.Today.ToString("yyyy.MM.dd")}";
            Client = client;
            if (!IndexCreated && !Client.Indices.Exists(IndexName).Exists) CreateIndex(Client);
            IndexCreated = true;
        }

        /// <summary>
        ///     Drop
        /// </summary>
        public virtual async Task Delete(TEntity entity, CancellationToken token)
        {
            if (entity != null) await Client.DeleteAsync(new DeleteRequest<TEntity>(IndexName, entity.Id), token);
        }

        /// <summary>
        ///     Get by id
        /// </summary>
        public virtual async Task<TEntity> GetById(Guid id, CancellationToken token)
        {
            var query = await Client.SearchAsync<TEntity>(req => req
                .From(0)
                .Size(1)
                .Index(IndexName)
                .Query(q => q.Term(t => t.Field("_id").Value(id))), token);
            return query.Documents.FirstOrDefault();
        }

        public Task Save(TEntity entity, CancellationToken token) => Update(entity.Id, entity, token);

        public Task<TEntity> GetByIdWithoutEvents(Guid id, CancellationToken token) => throw new NotImplementedException();

        public Task<TEntity> GetByIdWithEvents(Guid id, CancellationToken token) => throw new NotImplementedException();

        public void Dispose()
        {
        }

        /// <summary>
        ///     Build elasticsearch index
        /// </summary>
        protected virtual void CreateIndex(ElasticClient client, int numberOfReplicas = 0, int numberOfShards = 1)
        {
            client.Indices.CreateAsync(IndexName, c => c
                .Settings(s =>
                    s.NumberOfReplicas(numberOfReplicas)
                        .NumberOfShards(numberOfShards)
                )
                .Map<TEntity>(m => m.AutoMap()));
        }

        /// <summary>
        ///     Create
        /// </summary>
        public virtual async Task Add(TEntity entity, CancellationToken token)
        {
            var res = await Client.IndexAsync(entity, p => p
                .Index(IndexName)
                .Id(entity.Id), token);
            if (res.Result == Result.Error) throw new Exception(res.DebugInformation);
        }

        /// <summary>
        ///     Update single
        /// </summary>
        public virtual async Task<bool> Update(Guid id, TEntity entity, CancellationToken token)
        {
            await Delete(await GetById(id, token), token);
            await Add(entity, token);
            return true;
        }
    }
}
