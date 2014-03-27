using System.Threading.Tasks;
using Microsoft.AspNet.Routing;

namespace RoutingSample.Web
{
    public class DelegateRouteEndpoint : IRouter
    {
        public delegate Task RoutedDelegate(RouteContext context);

        private readonly RoutedDelegate _appFunc;

        public DelegateRouteEndpoint(RoutedDelegate appFunc)
        {
            _appFunc = appFunc;
        }

        public async Task RouteAsync(RouteContext context)
        {
            await _appFunc(context);
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
