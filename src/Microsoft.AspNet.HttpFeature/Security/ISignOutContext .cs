using System.Collections.Generic;

namespace Microsoft.AspNet.HttpFeature.Security
{
    public interface ISignOutContext 
    {
        IList<string> AuthenticationTypes { get; }

        void Accept(string authenticationType, IDictionary<string, object> description);
    }
}