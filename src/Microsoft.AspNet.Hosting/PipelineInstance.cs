using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.Hosting.Builder;
using Microsoft.AspNet.Hosting.Server;

namespace Microsoft.AspNet.Hosting
{
    public class PipelineInstance : IDisposable
    {
        private readonly IHttpContextFactory _httpContextFactory;
        private readonly RequestDelegate _requestDelegate;

        public PipelineInstance(IHttpContextFactory httpContextFactory, RequestDelegate requestDelegate)
        {
            _httpContextFactory = httpContextFactory;
            _requestDelegate = requestDelegate;
        }

        public Task Invoke(object serverEnvironment)
        {
            var httpContext = _httpContextFactory.CreateHttpContext(serverEnvironment);
            return _requestDelegate(httpContext);
        }

        public void Dispose()
        {
            // TODO: application notification of disposal
        }
    }
}
