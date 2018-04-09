using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TestServer
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", _ => { /* Controlled below */ });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            AllowCorsForAnyLocalhostPort(app);
            app.UseMvc();
        }

        private static void AllowCorsForAnyLocalhostPort(IApplicationBuilder app)
        {
            // It's not enough just to return "Access-Control-Allow-Origin: *", because
            // browsers don't allow wildcards in conjunction with credentials. So we must
            // specify explicitly which origin we want to allow.
            app.Use((context, next) =>
            {
                if (context.Request.Headers.TryGetValue("origin", out var incomingOriginValue))
                {
                    var origin = incomingOriginValue.ToArray()[0];
                    if (origin.StartsWith("http://localhost:") || origin.StartsWith("http://127.0.0.1:"))
                    {
                        context.Response.Headers.Add("Access-Control-Allow-Origin", origin);
                        context.Response.Headers.Add("Access-Control-Allow-Credentials", "true");
                        context.Response.Headers.Add("Access-Control-Allow-Methods", "HEAD,GET,PUT,POST,DELETE,OPTIONS");
                        context.Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type,TestHeader,another-header");
                        context.Response.Headers.Add("Access-Control-Expose-Headers", "MyCustomHeader,TestHeader,another-header");
                    }
                }

                return next();
            });
        }
    }
}
