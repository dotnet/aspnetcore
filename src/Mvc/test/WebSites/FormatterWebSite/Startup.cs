// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FormatterWebSite;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers(options =>
        {
            options.ModelMetadataDetailsProviders.Add(new SuppressChildValidationMetadataProvider(typeof(Developer)));
            options.ModelMetadataDetailsProviders.Add(new SuppressChildValidationMetadataProvider(typeof(Supplier)));

            options.InputFormatters.Add(new StringInputFormatter());
        })
        .AddNewtonsoftJson(options => options.SerializerSettings.Converters.Insert(0, new IModelConverter()))
        .AddXmlDataContractSerializerFormatters();
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseDeveloperExceptionPage();

        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapDefaultControllerRoute();
        });
    }
}

