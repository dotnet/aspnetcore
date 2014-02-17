using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc
{
    public interface IActionDescriptorProvider
    {
        IEnumerable<ActionDescriptor> GetDescriptors();
    }
}
