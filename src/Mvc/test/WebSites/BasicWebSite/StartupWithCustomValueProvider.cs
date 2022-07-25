// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BasicWebSite.ValueProviders;

namespace BasicWebSite;

public class StartupWithCustomValueProvider
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddMvc(o =>
            {
                o.ValueProviderFactories.Add(new CustomValueProviderFactory());
            });
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseDeveloperExceptionPage();

        app.UseRouting();

        app.UseEndpoints((endpoints) => endpoints.MapDefaultControllerRoute());
    }
}
