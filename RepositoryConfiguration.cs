using Microsoft.Extensions.DependencyInjection;
using Nest;
using System;

namespace ElasticLogger
{
    public abstract class RepositoryConfiguration
    {
        /// <summary>
        ///     Abstração para configuração do servidor de banco de dados
        /// </summary>
        /// <param name="serviceCollection">IoC</param>
        /// <param name="repositoryType">Tipo base dos repositórios</param>
        protected RepositoryConfiguration(IServiceCollection serviceCollection,
            Type repositoryType)
        {
            ServiceCollection = serviceCollection;
            RepositoryType = repositoryType;
        }

        protected IServiceCollection ServiceCollection { get; }

        /// <summary>
        ///     Tipo abstração para repositórios
        /// </summary>
        protected Type RepositoryType { get; }

        /// <summary>
        ///     Registra um repositório
        /// </summary>
        public virtual RepositoryConfiguration AddRepository<TInterface, TRepository>()
            where TRepository : TInterface
        {
            if (typeof(TRepository).IsAssignableFrom(RepositoryType))
                throw new NotSupportedException(typeof(TRepository).FullName);

            //Assinatira do Repositório com o Evento
            ServiceCollection.AddTransient(typeof(TInterface), typeof(TRepository));

            foreach (var @interface in typeof(TRepository)
                .GetInterfaces()
                .Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IRepository<>)))
            {
                //Cria ou registra a coleção vinculada
                var entity = @interface.GetGenericArguments()[0];
                RunMigration(entity);
            }

            return this;
        }

        /// <summary>
        ///     Migration
        /// </summary>
        protected abstract void RunMigration(Type entity);
    }
}