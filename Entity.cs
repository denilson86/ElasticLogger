using ElasticLogger.Serialize;
using Newtonsoft.Json;

namespace ElasticLogger
{
    public class Entity
    {
        [JsonIgnore]
        public virtual List<DomainEvent> DomainEvents { get; } = new List<DomainEvent>();

        [JsonProperty("id")]
        public virtual Guid Id { get; set; }

        [JsonProperty("deleted")]
        public virtual bool IsDeleted { get; set; }

        public virtual IEnumerable<TDomainEvent> GetDomains<TDomainEvent>() where TDomainEvent : DomainEvent
            => DomainEvents
                .OfType<TDomainEvent>()
                .OrderByDescending(de => de.CreatedAt);

        public static Guid GenerateId(string unique) => ProtectedSerializer.CreateMd5Hash(unique);
    }
}