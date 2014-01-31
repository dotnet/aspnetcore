using Microsoft.AspNet.Abstractions;

namespace Microsoft.AspNet.PipelineCore
{
    public interface ICanHasRequestCookies
    {
        IReadableStringCollection Cookies { get; }
    }
}