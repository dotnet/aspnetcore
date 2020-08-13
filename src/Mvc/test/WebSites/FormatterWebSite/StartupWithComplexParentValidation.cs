// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace FormatterWebSite
{
    public class StartupWithComplexParentValidation
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddControllers(options => options.ValidateComplexTypesIfChildValidationFails = true)
                .AddNewtonsoftJson(options => options.SerializerSettings.Converters.Insert(0, new IModelConverter()))
                .SetCompatibilityVersion(CompatibilityVersion.Latest);
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute();
            });
        }
    }
}