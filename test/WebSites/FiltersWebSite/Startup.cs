// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Authorization;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Mvc;
using Microsoft.Framework.DependencyInjection;

namespace FiltersWebSite
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app)
        {
            var configuration = app.GetTestConfiguration();

            app.UseServices(services =>
            {
                services.AddMvc(configuration);
                services.ConfigureAuthorization(options =>
                {
                    // This policy cannot succeed since it has no requirements
                    options.AddPolicy("Impossible", policy => { });
                    options.AddPolicy("Api", policy =>
                    {
                        policy.ActiveAuthenticationSchemes.Add("Api");
                        policy.RequireClaim(ClaimTypes.NameIdentifier);
                    });
                    options.AddPolicy("Api-Manager", policy =>
                    {
                        policy.ActiveAuthenticationSchemes.Add("Api");
                        policy.Requirements.Add(Operations.Edit);
                    });
                    options.AddPolicy("Interactive", policy =>
                    {
                        policy.ActiveAuthenticationSchemes.Add("Interactive");
                        policy.RequireClaim(ClaimTypes.NameIdentifier)
                              .RequireClaim("Permission", "CanViewPage");
                    });
                });
                services.AddSingleton<RandomNumberFilter>();
                services.AddSingleton<RandomNumberService>();
                services.AddTransient<IAuthorizationHandler, ManagerHandler>();
                services.Configure<BasicOptions>(o => o.AuthenticationScheme = "Api", "Api");
                services.Configure<BasicOptions>(o => o.AuthenticationScheme = "Interactive", "Interactive");

                services.Configure<MvcOptions>(options =>
                {
                    options.Filters.Add(new GlobalExceptionFilter());
                    options.Filters.Add(new GlobalActionFilter());
                    options.Filters.Add(new GlobalResultFilter());
                    options.Filters.Add(new GlobalAuthorizationFilter());
                    options.Filters.Add(new TracingResourceFilter("Global Resource Filter"));
                });
            });

            app.UseErrorReporter();

            app.UseMiddleware<AuthorizeBasicMiddleware>("Interactive");
            app.UseMiddleware<AuthorizeBasicMiddleware>("Api");

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
