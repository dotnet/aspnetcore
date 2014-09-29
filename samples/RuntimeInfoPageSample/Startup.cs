using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Diagnostics;
using Microsoft.AspNet.Http;

namespace RuntimeInfoPageSample
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app)
        {
            app.UseRuntimeInfoPage();

            app.Run(context =>
            {
                context.Response.StatusCode = 302;
                context.Response.Headers["Location"] = "/runtimeinfo";

                return Task.FromResult(0);
            });
        }
    }
}
