using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.PipelineCore.Collections;

namespace Microsoft.AspNet.PipelineCore
{
    public interface ICanHasResponseCookies
    {
        IResponseCookiesCollection Cookies { get; }
    }
}