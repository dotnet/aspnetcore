using System.Collections.Generic;
using Microsoft.Net.Runtime;

namespace Microsoft.AspNet.HttpFeature.Security
{
    [AssemblyNeutral]
    public interface ISignOutContext 
    {
        IList<string> AuthenticationTypes { get; }

        void Accept(string authenticationType, IDictionary<string, object> description);
    }
}