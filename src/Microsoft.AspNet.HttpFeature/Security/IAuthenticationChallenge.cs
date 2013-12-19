using System.Collections.Generic;

namespace Microsoft.AspNet.Interfaces
{
    public interface IAuthenticationChallenge
    {
        IEnumerable<string> AuthenticationTypes { get; }
        IDictionary<string, string> Properties { get; }
    }
}