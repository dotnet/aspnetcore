
using System;
#if NET45
using Owin;
#endif

using AppFunc = System.Func<System.Collections.Generic.IDictionary<string, object>, System.Threading.Tasks.Task>;

namespace Microsoft.AspNet.Routing
{
    public static class RouteBuilderExtensions
    {
#if NET45
        public static IRouteEndpoint ForApp(this IRouteBuilder routeBuilder, Func<IAppBuilder, IRouteEngine, IAppBuilder> handlerBuilder)
        {
            var builder = handlerBuilder.Invoke(routeBuilder.AppBuilder.New(), routeBuilder.Engine);
            var appFunc = (AppFunc)builder.Build(typeof(AppFunc));
            return routeBuilder.ForApp(() => appFunc);
        }

        public static IRouteEndpoint ForApp(this IRouteBuilder routeBuilder, Action<IAppBuilder, IRouteEngine> handlerBuilder)
        {
            return routeBuilder.ForApp((builder, engine) => { handlerBuilder(builder, engine); return builder; });
        }

        public static IRouteEndpoint ForApp(this IRouteBuilder routeBuilder, Action<IAppBuilder> handlerBuilder)
        {
            return routeBuilder.ForApp((builder, engine) => { handlerBuilder(builder); return builder; });
        }

        public static IRouteEndpoint ForApp(this IRouteBuilder routeBuilder, Func<IAppBuilder, IAppBuilder> handlerBuilder)
        {
            return routeBuilder.ForApp((builder, engine) => handlerBuilder(builder));
        }
#endif
        public static IRouteEndpoint ForApp(this IRouteBuilder routeBuilder, Func<AppFunc, AppFunc> handlerBuilder)
        {
            return routeBuilder.ForApp(() => handlerBuilder(null));
        }

        public static IRouteEndpoint ForApp(this IRouteBuilder routeBuilder, AppFunc handler)
        {
            return routeBuilder.ForApp((Func<AppFunc, AppFunc>)((next) => handler));
        }
    }
}
