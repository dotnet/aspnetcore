using System.Collections.Generic;

namespace Microsoft.AspNet.Interfaces.Security
{
    public interface IAuthenticationDescription
    {
        IDictionary<string, object> Properties { get; set; }
    }
}