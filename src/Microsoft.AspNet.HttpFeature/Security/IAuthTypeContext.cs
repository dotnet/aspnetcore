using System.Collections.Generic;
using Microsoft.Net.Runtime;

namespace Microsoft.AspNet.HttpFeature.Security
{
    [AssemblyNeutral]
    public interface IAuthTypeContext
    {
        void Accept(IDictionary<string,object> description);
    }
}