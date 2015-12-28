// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Formatters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;

namespace FormatFilterSample.Web
{
    public class Startup
    {
        // Set up application services
        public void ConfigureServices(IServiceCollection services)
        {
            var mvcBuilder = services.AddMvc(options =>
            {
                var formatFilter = new FormatFilterAttribute();
                options.Filters.Add(formatFilter);

                var customFormatter = new CustomFormatter("application/custom");
                options.OutputFormatters.Add(customFormatter);
                options.OutputFormatters.RemoveType<StringOutputFormatter>();

                options.FormatterMappings.SetMediaTypeMappingForFormat(
                    "custom",
                    MediaTypeHeaderValue.Parse("application/custom"));
            });

            mvcBuilder.AddXmlDataContractSerializerFormatters();
        }

        public void Configure(IApplicationBuilder app)
        {
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