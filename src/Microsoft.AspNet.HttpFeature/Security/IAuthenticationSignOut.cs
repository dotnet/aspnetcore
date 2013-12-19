using System.Collections.Generic;

namespace Microsoft.AspNet.Interfaces.Security
{
    public interface IAuthenticationSignOut
    {
        IEnumerable<string> AuthenticationTypes { get; }
    }
}