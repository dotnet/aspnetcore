using System.Collections.Generic;
using System.Security.Claims;

namespace Microsoft.AspNet.Interfaces.Security
{
    public interface IAuthenticationResult
    {
        ClaimsIdentity Identity { get; }
        IDictionary<string, object> Properties { get; }
        IAuthenticationDescription Description { get; }
    }
}
