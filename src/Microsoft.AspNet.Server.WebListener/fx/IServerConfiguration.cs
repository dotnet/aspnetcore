using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Net.Runtime;

namespace Microsoft.AspNet.Hosting.Server
{
    [AssemblyNeutral]
    public interface IServerConfiguration
    {
        IList<IDictionary<string, object>> Addresses { get; }
        object AdvancedConfiguration { get; }
    }
}
