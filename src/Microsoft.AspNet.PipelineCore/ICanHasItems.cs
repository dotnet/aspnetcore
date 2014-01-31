using System.Collections.Generic;

namespace Microsoft.AspNet.PipelineCore
{
    public interface ICanHasItems
    {
        IDictionary<object, object> Items { get; }
    }
}