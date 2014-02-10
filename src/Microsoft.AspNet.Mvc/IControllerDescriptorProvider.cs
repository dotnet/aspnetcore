using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc
{
    public interface IControllerDescriptorProvider
    {
        IEnumerable<ControllerDescriptor> GetControllers(string controllerName);
    }
}
