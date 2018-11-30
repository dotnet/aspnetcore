// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using LocalizationWebsite.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace LocalizationWebsite
{
    public class StartupBuilderAPIs
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLocalization(options => options.ResourcesPath = "Resources");
        }

        public void Configure(
            IApplicationBuilder app,
            ILoggerFactory loggerFactory,
            IStringLocalizer<Customer> customerStringLocalizer)
        {
            var supportedCultures = new[] { "en-US", "fr-FR" };
            app.UseRequestLocalization(options =>
                options
                    .AddSupportedCultures(supportedCultures)
                    .AddSupportedUICultures(supportedCultures)
                    .SetDefaultCulture("ar-YE")
            );

            app.Run(async (context) =>
            {
                var requestCultureFeature = context.Features.Get<IRequestCultureFeature>();
                var requestCulture = requestCultureFeature.RequestCulture;
                await context.Response.WriteAsync(customerStringLocalizer["Hello"]);
            });
        }
    }
}
