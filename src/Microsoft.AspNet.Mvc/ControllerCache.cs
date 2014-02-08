using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc
{
    public abstract class ControllerCache
    {
        public abstract IEnumerable<ControllerDescriptor> GetController(string controllerName);
    }
}
