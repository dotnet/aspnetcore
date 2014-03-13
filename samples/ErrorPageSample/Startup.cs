#if NET45
using System;
using Microsoft.AspNet;
using Microsoft.AspNet.Abstractions;
using Owin;

namespace ErrorPageSample
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // Temporary bridge from katana to Owin
            app.UseBuilder(ConfigurePK);
        }

        private void ConfigurePK(IBuilder builder)
        {
            builder.UseErrorPage();
            builder.Run(context =>
            {
                throw new Exception("Demonstration exception");
            });
        }
    }
}
#endif