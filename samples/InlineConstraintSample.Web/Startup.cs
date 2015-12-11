// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using InlineConstraintSample.Web.Constraints;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Localization;
using Microsoft.Extensions.DependencyInjection;

namespace InlineConstraintSample.Web
{
    public class Startup
    {
        // Set up application services
        public void ConfigureServices(IServiceCollection services)
        {
            services.ConfigureRouting(
                routeOptions => routeOptions.ConstraintMap.Add(
                    "IsbnDigitScheme10",
                    typeof(IsbnDigitScheme10Constraint)));

            services.ConfigureRouting(
                routeOptions => routeOptions.ConstraintMap.Add(
                    "IsbnDigitScheme13",
                    typeof(IsbnDigitScheme10Constraint)));

            // Update an existing constraint from ConstraintMap for test purpose.
            services.ConfigureRouting(
                routeOptions =>
                {
                    if (routeOptions.ConstraintMap.ContainsKey("IsbnDigitScheme13"))
                    {
                        routeOptions.ConstraintMap["IsbnDigitScheme13"] = typeof(IsbnDigitScheme13Constraint);
                    }
                });

            // Add MVC services to the services container
            services.AddMvc();
        }

        public void Configure(IApplicationBuilder app)
        {
            // Ignore ambient and client locale. Use same values as ReplaceCultureAttribute / CultureReplacerMiddleware.
            var localizationOptions = new RequestLocalizationOptions();
            localizationOptions.RequestCultureProviders.Clear();
            app.UseRequestLocalization(localizationOptions, new RequestCulture("en-GB", "en-US"));

            app.UseMvc();
        }
    }
}
