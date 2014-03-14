using System.Threading.Tasks;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.Routing;

namespace RoutingSample.Web
{
    public class HttpContextRouteEndpoint : IRouter
    {
        private readonly RequestDelegate _appFunc;

        public HttpContextRouteEndpoint(RequestDelegate appFunc)
        {
            _appFunc = appFunc;
        }

        public async Task RouteAsync(RouteContext context)
        {
            await _appFunc(context.HttpContext);
            context.IsHandled = true;
        }

        public string GetVirtualPath(VirtualPathContext context)
        {
            // We don't really care what the values look like.
            context.IsBound = true;
            return null;
        }
    }
}
