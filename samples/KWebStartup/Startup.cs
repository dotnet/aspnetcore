using Microsoft.AspNet.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KWebStartup
{
    public class Startup
    {
        public void Configuration(IBuilder app)
        {
            app.Run(async context =>
            {
                context.Response.ContentType = "text/plain";
                await context.Response.WriteAsync("Hello world");
            });
        }
    }
}