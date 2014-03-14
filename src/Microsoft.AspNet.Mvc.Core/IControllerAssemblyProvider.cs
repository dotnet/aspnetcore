using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.AspNet.Mvc
{
    public interface IControllerAssemblyProvider
    {
        IEnumerable<Assembly> CandidateAssemblies { get; }
    }
}
