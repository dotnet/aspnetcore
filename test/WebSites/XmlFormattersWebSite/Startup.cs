// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;

namespace XmlFormattersWebSite
{
    public class Startup
    {
        // Set up application services
        public void ConfigureServices(IServiceCollection services)
        {
            // Add MVC services to the services container
            services.AddMvc();

            services.Configure<MvcOptions>(options =>
            {
                options.InputFormatters.Clear();
                options.OutputFormatters.Clear();

                // Since both XmlSerializer and DataContractSerializer based formatters
                // have supported media types of 'application/xml' and 'text/xml',  it 
                // would be difficult for a test to choose a particular formatter based on
                // request information (Ex: Accept header).
                // So here we instead clear out the default supported media types and create new
                // ones which are distinguishable between formatters.
                var xmlSerializerInputFormatter = new XmlSerializerInputFormatter(new MvcOptions());
                xmlSerializerInputFormatter.SupportedMediaTypes.Clear();
                xmlSerializerInputFormatter.SupportedMediaTypes.Add(
                    new MediaTypeHeaderValue("application/xml-xmlser"));
                xmlSerializerInputFormatter.SupportedMediaTypes.Add(
                    new MediaTypeHeaderValue("text/xml-xmlser"));

                var xmlSerializerOutputFormatter = new XmlSerializerOutputFormatter();
                xmlSerializerOutputFormatter.SupportedMediaTypes.Clear();
                xmlSerializerOutputFormatter.SupportedMediaTypes.Add(
                    new MediaTypeHeaderValue("application/xml-xmlser"));
                xmlSerializerOutputFormatter.SupportedMediaTypes.Add(
                    new MediaTypeHeaderValue("text/xml-xmlser"));

                var dcsInputFormatter = new XmlDataContractSerializerInputFormatter(new MvcOptions());
                dcsInputFormatter.SupportedMediaTypes.Clear();
                dcsInputFormatter.SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/xml-dcs"));
                dcsInputFormatter.SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/xml-dcs"));

                var dcsOutputFormatter = new XmlDataContractSerializerOutputFormatter();
                dcsOutputFormatter.SupportedMediaTypes.Clear();
                dcsOutputFormatter.SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/xml-dcs"));
                dcsOutputFormatter.SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/xml-dcs"));

                options.InputFormatters.Add(dcsInputFormatter);
                options.InputFormatters.Add(xmlSerializerInputFormatter);
                options.OutputFormatters.Add(dcsOutputFormatter);
                options.OutputFormatters.Add(xmlSerializerOutputFormatter);

                xmlSerializerInputFormatter.WrapperProviderFactories.Add(new PersonWrapperProviderFactory());
                xmlSerializerOutputFormatter.WrapperProviderFactories.Add(new PersonWrapperProviderFactory());
                dcsInputFormatter.WrapperProviderFactories.Add(new PersonWrapperProviderFactory());
                dcsOutputFormatter.WrapperProviderFactories.Add(new PersonWrapperProviderFactory());
            });
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseMvc(routes =>
            {
                routes.MapRoute("ActionAsMethod", "{controller}/{action}",
                    defaults: new { controller = "Home", action = "Index" });
            });
        }

        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<Startup>()
                .UseKestrel()
                .UseIISIntegration()
                .Build();

            host.Run();
        }
    }
}

