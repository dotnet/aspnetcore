using System.Collections.Generic;

namespace Microsoft.AspNet.PipelineCore
{
    public class DefaultCanHasItems : ICanHasItems
    {
        public DefaultCanHasItems()
        {
            Items = new Dictionary<object, object>();
        }

        public IDictionary<object, object> Items { get; private set; }
    }
}