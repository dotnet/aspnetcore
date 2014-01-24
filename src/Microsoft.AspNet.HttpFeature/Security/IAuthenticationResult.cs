using System.Collections.Generic;
#if NET45
using System.Security.Claims;
#endif
using Microsoft.AspNet.HttpFeature.Security;

// ReSharper disable once CheckNamespace
namespace Microsoft.AspNet.Interfaces.Security
{
    public interface IAuthenticationResult
    {
#if NET45
        ClaimsIdentity Identity { get; }
#else
        
#endif
        IDictionary<string, object> Properties { get; }
        IAuthenticationDescription Description { get; }
    }
}
