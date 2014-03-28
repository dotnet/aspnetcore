using System;
using Microsoft.AspNet;
using Microsoft.AspNet.Abstractions;

namespace ErrorPageSample
{
    public class Startup
    {
        public void Configuration(IBuilder app)
        {
            app.UseErrorPage();
            app.Run(context =>
            {
                throw new Exception("Demonstration exception");
            });
        }
    }
}
