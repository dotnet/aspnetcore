// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BasicWebSite.Controllers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace BasicWebSite
{
    public class StartupClientValidationSettings
    {
        // Set up application services
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRouting();

            services.AddMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Latest)
                .AddNewtonsoftJson()
                .AddXmlDataContractSerializerFormatters()
                .AddViewOptions(o => o.HtmlHelperOptions.ClientValidationEnabled = false)
                .ConfigureApplicationPartManager(apm => {
                    var controller = apm.FeatureProviders
                    .Single(f => f is IApplicationFeatureProvider<ControllerFeature>);
                    apm.FeatureProviders.Remove(controller);

                    apm.FeatureProviders.Add(new ControllerListFeatureProvider());
                });

            services.ConfigureBaseWebSiteAuthPolicies();

            services.AddHttpContextAccessor();
            services.AddScoped<RequestIdService>();
            services.AddScoped<TestResponseGenerator>();
            services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseDeveloperExceptionPage();

            // Initializes the RequestId service for each request
            app.UseMiddleware<RequestIdMiddleware>();

            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapControllerRoute("ClientValidationDisabled",
                    "Controller/ClientValidationDisabled",
                    new
                    {
                        controller = "ClientValidationDisabled",
                        action = nameof(ClientValidationDisabledController.ValidationDisabled)
                    });
            });
        }

        private class ControllerListFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
        {
            public void PopulateFeature(
                IEnumerable<ApplicationPart> parts, ControllerFeature feature)
            {
                var typeInfo = typeof(ClientValidationDisabledController)
                    .GetTypeInfo();

                if (!feature.Controllers.Contains(typeInfo))
                {
                    feature.Controllers.Add(typeInfo);
                }
            }
        }
    }
}
