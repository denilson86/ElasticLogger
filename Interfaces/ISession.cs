using System.Security.Claims;

namespace ElasticLogger.Interfaces
{
    public interface ISession
    {
        byte[] UserSignature { get; set; }

        byte[] SessionSignature { get; set; }

        Guid Application { get; set; }

        int Version { set; get; }

        string PushId { get; set; }

        string Jwe { get; set; }

        Guid Device { get; set; }

        Guid User { get; set; }

        bool Authenticated { get; }

        bool NeedToChangePassword { get; set; }

        bool FirstAccess { get; set; }

        string TokenOfd { get; set; }

        Dictionary<string, string> ExternalAuths { get; }

        IEnumerable<Claim> ToClaims();
    }
}