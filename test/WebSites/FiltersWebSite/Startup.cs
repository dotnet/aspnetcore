// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Claims;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Security;
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
                services.Configure<AuthorizationOptions>(options =>
                {
                    // This policy cannot succeed since it has no requirements
                    options.AddPolicy("Impossible",
                        new AuthorizationPolicyBuilder()
                            .Build());
                    options.AddPolicy("RequireBasic", 
                        new AuthorizationPolicyBuilder("Basic")
                            .RequiresClaim(ClaimTypes.NameIdentifier)
                            .Build());
                    options.AddPolicy("CanViewPage", 
                        new AuthorizationPolicyBuilder()
                            .RequiresClaim("Permission", "CanViewPage")
                            .Build());
                });
                services.AddSingleton<RandomNumberFilter>();
                services.AddSingleton<RandomNumberService>();

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

            app.UseMiddleware<AuthorizeBasicMiddleware>();

            app.UseMvc();
        }
    }
}
