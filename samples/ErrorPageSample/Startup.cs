using System;
using Microsoft.AspNet.Builder;

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
