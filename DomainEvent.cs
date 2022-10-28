using ElasticLogger.Interfaces;
using Newtonsoft.Json;

namespace ElasticLogger
{
    public class DomainEvent : EventArgs
    {
        public new static readonly DomainEvent Empty = new EmptyDomainEvent();

        [JsonProperty("id")]
        public virtual Guid Id { get; set; }

        [JsonProperty("channel")]
        public virtual string Push { get; set; }

        [JsonProperty("created_at")]
        public virtual DateTimeOffset CreatedAt { get; set; }

        [JsonProperty("created_by")]
        public virtual Guid? CreatedBy { get; set; }

        [JsonProperty("store_at")]
        public virtual DateTime? StoreAt { get; set; }

        public static TDomainEvent CreateDomainEvent<TDomainEvent>(ISession session = null) where TDomainEvent : DomainEvent, new() =>
            new TDomainEvent
            {
                Id = Guid.NewGuid(),
                CreatedBy = session == null ? Guid.Empty : session.User == Guid.Empty ? new Guid(session.UserSignature) : session.User,
                CreatedAt = DateTime.Now,
                Push = session == null ? string.Empty : session.PushId,
            };

        public static TDomainEvent CreateDomainEvent<TDomainEvent>(DomainEvent owner) where TDomainEvent : DomainEvent, new() =>
            new TDomainEvent
            {
                Id = Guid.NewGuid(),
                CreatedBy = owner.CreatedBy,
                CreatedAt = DateTime.Now,
                Push = owner.Push,
            };


        private class EmptyDomainEvent : DomainEvent
        {
        }
    }
}