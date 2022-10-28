using Amazon;
using Amazon.Elasticsearch;
using Amazon.Runtime;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nest;

namespace ElasticLogger
{
    public class ElasticSearchConfiguration : RepositoryConfiguration
    {
        /// <summary>
        ///     Configuração para ElasticSearch
        /// </summary>
        /// <param name="serviceCollection">Container de injeção de dependencia</param>
        internal ElasticSearchConfiguration(IServiceCollection serviceCollection)
            : base(serviceCollection, typeof(ElasticsearchRepository<>))
        {
            ServiceCollection.AddSingleton(this);
        }

        /// <summary>
        ///     Parâmetros de Configuração
        /// </summary>
        public ConnectionSettings Settings { get; set; }

        /// <summary>
        ///     Endpoint de acesso ao elasticsearch da amazon
        /// </summary>
        protected internal AmazonElasticsearchClient AmazonElasticsearchClient { get; set; }

        /// <summary>
        ///     Logger
        /// </summary>
        protected internal static ILogger Logger { get; internal set; }

        /// <summary>
        ///     Adiciona configurações de repositórios
        /// </summary>
        public new ElasticSearchConfiguration AddRepository<TInterface, TRepository>()
            where TRepository : TInterface =>
            base.AddRepository<TInterface, TRepository>() as ElasticSearchConfiguration;

        /// <summary>
        ///     Configura a conexão com o ElasticSearch
        /// </summary>
        public ElasticSearchConfiguration ConfigureConnectionFactory(string uriString)
        {
            Settings = new ConnectionSettings(new Uri(uriString))
                .DisableDirectStreaming();

            ServiceCollection.AddTransient(_ => new ElasticClient(Settings));
            return this;
        }

        /// <summary>
        ///     Configura a conexão com o ElasticSearch
        /// </summary>
        public ElasticSearchConfiguration ConfigureOnAws(string region, string accessKey, string secretKey)
        {
            ServiceCollection.AddSingleton(AmazonElasticsearchClient = new AmazonElasticsearchClient(
                new BasicAWSCredentials(accessKey, secretKey),
                new AmazonElasticsearchConfig
                {
                    RegionEndpoint = RegionEndpoint.GetBySystemName(region),
                }));
            return this;
        }

        protected override void RunMigration(Type entity)
        {
        }

        public (bool status, string message) GetServiceStatus(HttpContext context)
        {
            try
            {
                var elasticClient = context.RequestServices.GetService<ElasticClient>();
                var response = elasticClient.Ping(p => p.Human());
                Logger?.LogInformation("Teste de acesso ao elasticsearch acionado com sucesso.");
                return (true, response.DebugInformation);
            }
            catch (Exception exception)
            {
                Logger?.LogError(exception, $"Teste de acesso ao elasticsearch acionado com falha: {exception}");
                return (false, $"FAIL: {exception.Message}");
            }
        }
    }
}