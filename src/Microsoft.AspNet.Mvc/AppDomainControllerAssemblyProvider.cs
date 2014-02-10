using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Microsoft.AspNet.Mvc
{
    public class AppDomainControllerAssemblyProvider : ControllerAssemblyProvider
    {
        public IEnumerable<Assembly> Assemblies
        {
            get
            {
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().Where(AllowAssembly))
                {
                    yield return assembly;
                }
            }
        }

        private bool AllowAssembly(Assembly assembly)
        {
            // consider mechanisms to filter assemblies upfront, so scanning cost is minimized and startup improved.
            // 1 - Does assembly reference the WebFx assembly (directly or indirectly). - Down side, object only controller not supported.
            // 2 - Remove well known assemblies (maintenance and composability cost)
            return true;
        }
    }
}
