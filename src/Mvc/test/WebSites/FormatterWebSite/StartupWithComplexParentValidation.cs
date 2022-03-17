// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace FormatterWebSite;

public class StartupWithComplexParentValidation
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddControllers(options => options.ValidateComplexTypesIfChildValidationFails = true)
            .AddNewtonsoftJson(options => options.SerializerSettings.Converters.Insert(0, new IModelConverter()));
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
