
using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc
{
    public class ActionDescriptorProviderContext
    {
        public ActionDescriptorProviderContext()
        {
            ActionDescriptors = new List<ActionDescriptor>();
        }

        public List<ActionDescriptor> ActionDescriptors { get; private set; }
    }
}
