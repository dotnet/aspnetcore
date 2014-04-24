using System.Collections.Generic;

namespace Microsoft.AspNet.PipelineCore
{
    public class DefaultCanHasItems : ICanHasItems
    {
        public DefaultCanHasItems()
        {
            Items = new ItemsDictionary();
        }

        public IDictionary<object, object> Items { get; private set; }
    }
}