using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.PipelineCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.FeatureModel;
using Microsoft.AspNet.PipelineCore.Owin;

namespace Owin
{
    using AppFunc = Func<IDictionary<string, object>, Task>;

    public static class AppBuilderSupportExtensions
    {
        public static IAppBuilder UseBuilder(this IAppBuilder appBuilder, Action<IBuilder> configuration)
        {
            IBuilder builder = new Builder();
            configuration(builder);
            Func<AppFunc,AppFunc> middleware1 = next1 => {
                Func<RequestDelegate,RequestDelegate> middleware2 = next2 => {
                    return httpContext => {
                        return next1(httpContext.GetFeature<ICanHasOwinEnvironment>().Environment);
                    };
                };
                builder.Use(middleware2);
                var app = builder.Build();
                return env =>
                {
                    return app.Invoke(
                        new DefaultHttpContext(
                            new FeatureCollection(
                                new FeatureObject(
                                    new OwinHttpEnvironment(env)))));
                };
            };
            return appBuilder.Use(middleware1);
        }
    }
}
