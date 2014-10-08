// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Routing;
using Microsoft.Framework.DependencyInjection;

namespace ApiExplorer
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app)
        {
            var configuration = app.GetTestConfiguration();

            app.UseServices(services =>
            {
                services.AddMvc(configuration);
                services.AddSingleton<ApiExplorerDataFilter>();

                services.Configure<MvcOptions>(options =>
                {
                    options.Filters.AddService(typeof(ApiExplorerDataFilter));

                    options.ApplicationModelConventions.Add(new ApiExplorerVisibilityEnabledConvention());
                    options.ApplicationModelConventions.Add(new ApiExplorerVisibilityDisabledConvention(
                        typeof(ApiExplorerVisbilityDisabledByConventionController)));

                    options.OutputFormatters.Clear();
                    options.OutputFormatters.Add(new JsonOutputFormatter());
                    options.OutputFormatters.Add(new XmlDataContractSerializerOutputFormatter());
                });
            });

            app.UseMvc(routes =>
            {
                routes.MapRoute("default", "{controller}/{action}");
            });
        }
    }
}
