using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;

namespace WebApplication95
{
    public static class HttpDispatcherAppBuilderExtensions
    {
        public static IApplicationBuilder UseSockets(this IApplicationBuilder app, Action<HttpConnectionDispatcher> callback)
        {
            var dispatcher = new HttpConnectionDispatcher(app);
            callback(dispatcher);
            app.UseRouter(dispatcher.GetRouter());
            return app;
        }
    }

}
