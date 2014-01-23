using System.Collections.Generic;

namespace Microsoft.AspNet.HttpFeature.Security
{
    public interface IAuthenticationDescription
    {
        IDictionary<string, object> Properties { get; set; }
    }
}