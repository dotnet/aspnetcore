// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Mvc;
using Microsoft.Framework.DependencyInjection;
using ModelBindingWebSite.Models;
using ModelBindingWebSite.Services;

namespace ModelBindingWebSite
{
    public class Startup
    {
        // Set up application services
        public void ConfigureServices(IServiceCollection services)
        {
            // Add MVC services to the services container
            services.AddMvc()
                    .Configure<MvcOptions>(m =>
                    {
                        m.MaxModelValidationErrors = 8;
                        m.ModelBinders.Insert(0, new TestBindingSourceModelBinder());

                        m.AddXmlDataContractSerializerFormatter();
                        m.ValidationExcludeFilters.Add(typeof(Address));

                            // ModelMetadataController relies on additional values AdditionalValuesMetadataProvider provides.
                            m.ModelMetadataDetailsProviders.Add(new AdditionalValuesMetadataProvider());
                    });

            services.AddSingleton<ICalculator, DefaultCalculator>();
            services.AddSingleton<ITestService, TestService>();

            services.AddTransient<IVehicleService, VehicleService>();
            services.AddTransient<ILocationService, LocationService>();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseCultureReplacer();

            app.UseErrorReporter();

            // Add MVC to the request pipeline
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
