using System.Collections.Generic;

namespace Microsoft.AspNet.HttpFeature.Security
{
    public interface IAuthenticationChallenge
    {
        IEnumerable<string> AuthenticationTypes { get; }
        IDictionary<string, string> Properties { get; }
    }
}