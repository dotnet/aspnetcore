using Microsoft.AspNet.Abstractions;

namespace Microsoft.AspNet.PipelineCore
{
    public interface ICanHasQuery
    {
        IReadableStringCollection Query { get; }
    }
}
