using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.AspNet.Mvc
{
    public interface ControllerAssemblyProvider
    {
        IEnumerable<Assembly> Assemblies { get; }
    }
}
