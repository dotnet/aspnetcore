using Microsoft.AspNet.Abstractions;

namespace Microsoft.AspNet.Hosting.Builder
{
    public interface IHttpContextFactory
    {
        HttpContext CreateHttpContext(object serverContext);
    }
}