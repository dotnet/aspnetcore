// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using InlineConstraintSample.Web.Constraints;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Localization;
using Microsoft.Extensions.DependencyInjection;

namespace InlineConstraintSample.Web
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRouting( routeOptions =>
            {
                routeOptions.ConstraintMap.Add("IsbnDigitScheme10", typeof(IsbnDigitScheme10Constraint));
                routeOptions.ConstraintMap.Add("IsbnDigitScheme13", typeof(IsbnDigitScheme13Constraint));
            });

            // Add MVC services to the services container
            services.AddMvc();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseRequestLocalization(new RequestLocalizationOptions
            {
                // Ignore ambient and client locale. Use same values as ReplaceCultureAttribute / CultureReplacerMiddleware.
                DefaultRequestCulture = new RequestCulture("en-GB", "en-US"),
                RequestCultureProviders = new List<IRequestCultureProvider>()
            });

            app.UseMvc();
        }

        public static void Main(string[] args)
        {
            var application = new WebApplicationBuilder()
                .UseConfiguration(WebApplicationConfiguration.GetDefault(args))
                .UseStartup<Startup>()
                .Build();

            application.Run();
        }
    }
}
