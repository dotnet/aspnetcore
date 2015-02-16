// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.Framework.DependencyInjection;
using ModelBindingWebSite.Services;

namespace ModelBindingWebSite
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app)
        {
            var configuration = app.GetTestConfiguration();

            // Set up application services
            app.UseServices(services =>
            {
                // Override the IModelMetadataProvider AddMvc would normally add.
                // ModelMetadataController relies on additional values AdditionalValuesMetadataProvider provides.
                services.AddSingleton<IModelMetadataProvider, AdditionalValuesMetadataProvider>();

                // Add MVC services to the services container
                services.AddMvc(configuration)
                        .Configure<MvcOptions>(m =>
                        {
                            m.MaxModelValidationErrors = 8;
                            m.ModelBinders.Insert(0, typeof(TestBindingSourceModelBinder));

                            m.InputFormatters.InstanceOf<JsonInputFormatter>().CaptureDeserilizationErrors = true;

                            m.AddXmlDataContractSerializerFormatter();
                            m.ValidationExcludeFilters.Add(typeof(Address));
                        });

                services.AddSingleton<ICalculator, DefaultCalculator>();
                services.AddSingleton<ITestService, TestService>();

                services.AddTransient<IVehicleService, VehicleService>();
                services.AddTransient<ILocationService, LocationService>();
            });

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
