
using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc
{
    public class ActionDescriptorProviderContext
    {
        public ActionDescriptorProviderContext()
        {
            Results = new List<ActionDescriptor>();
        }

        public List<ActionDescriptor> Results { get; private set; }
    }
}
