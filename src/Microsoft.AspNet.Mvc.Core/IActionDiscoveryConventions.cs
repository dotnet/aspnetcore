using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.AspNet.Mvc
{
    public interface IActionDiscoveryConventions
    {
        bool IsController(TypeInfo typeInfo);

        IEnumerable<ActionInfo> GetActions(MethodInfo methodInfo);
    }
}
