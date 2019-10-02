using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
            services.AddMvc().AddNewtonsoftJson();
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", _ => { /* Controlled below */ });
            });
            services.AddServerSideBlazor()
                .AddCircuitOptions(o =>
                {
                    var detailedErrors = Configuration.GetValue<bool>("circuit-detailed-errors");
                    o.DetailedErrors = detailedErrors;
                });
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie();

            services.AddAuthorization(options =>
            {
                options.AddPolicy("NameMustStartWithB", policy =>
                    policy.RequireAssertion(ctx => ctx.User.Identity.Name?.StartsWith("B") ?? false));
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // It's not enough just to return "Access-Control-Allow-Origin: *", because
            // browsers don't allow wildcards in conjunction with credentials. So we must
            // specify explicitly which origin we want to allow.
            app.UseCors(policy =>
            {
                policy.SetIsOriginAllowed(host => host.StartsWith("http://localhost:") || host.StartsWith("http://127.0.0.1:"))
                    .AllowAnyHeader()
                    .WithExposedHeaders("MyCustomHeader")
                    .AllowAnyMethod()
                    .AllowCredentials();
            });

            app.UseAuthentication();

            // Mount the server-side Blazor app on /subdir
            app.Map("/subdir", app =>
            {
                app.UseStaticFiles();
                app.UseClientSideBlazorFiles<BasicTestApp.Startup>();

                app.UseRequestLocalization(options =>
                {
                    options.AddSupportedCultures("en-US", "fr-FR");
                    options.AddSupportedUICultures("en-US", "fr-FR");

                    // Cookie culture provider is included by default, but we want it to be the only one.
                    options.RequestCultureProviders.Clear();
                    options.RequestCultureProviders.Add(new CookieRequestCultureProvider());

                    // We want the default to be en-US so that the tests for bind can work consistently.
                    options.SetDefaultCulture("en-US");
                });

                app.MapWhen(ctx => ctx.Request.Cookies.TryGetValue("__blazor_execution_mode", out var value) && value == "server",
                    child =>
                    {
                        child.UseRouting();
                        child.UseEndpoints(childEndpoints =>
                        {
                            childEndpoints.MapBlazorHub();
                            childEndpoints.MapFallbackToPage("/_ServerHost");
                        });
                    });

                app.MapWhen(ctx => !ctx.Request.Query.ContainsKey("__blazor_execution_mode"),
                    child =>
                    {
                        child.UseRouting();
                        child.UseEndpoints(childEndpoints =>
                        {
                            childEndpoints.MapBlazorHub();
                            childEndpoints.MapFallbackToClientSideBlazor<BasicTestApp.Startup>("index.html");
                        });
                    });
            });

            // Separately, mount a prerendered server-side Blazor app on /prerendered
            app.Map("/prerendered", app =>
            {
                app.UsePathBase("/prerendered");
                app.UseStaticFiles();
                app.UseRouting();
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapFallbackToPage("/PrerenderedHost");
                    endpoints.MapBlazorHub();
                });
            });

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapRazorPages();

                // Redirect for convenience when testing locally since we're hosting the app at /subdir/
                endpoints.Map("/", context =>
                {
                    context.Response.Redirect("/subdir");
                    return Task.CompletedTask;
                });
            });
        }
    }
}
