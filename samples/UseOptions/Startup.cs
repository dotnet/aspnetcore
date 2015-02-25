using System;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.Framework.DependencyInjection;

namespace UseOptions
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors();
            services.ConfigureCors(
                options =>
                    options.AddPolicy("allowSingleOrigin", builder => builder.WithOrigins("http://example.com")));
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseCors("allowSingleOrigin");
        }
    }
}
