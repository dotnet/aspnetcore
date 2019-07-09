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
        public virtual CompatibilityVersion CompatibilityVersion => CompatibilityVersion.Latest;

        // Set up application services
        public void ConfigureServices(IServiceCollection services)
        {
            // Add MVC services to the services container
            services.AddControllers()
                .AddXmlDataContractSerializerFormatters()
                .AddXmlSerializerFormatters()
                .SetCompatibilityVersion(CompatibilityVersion);

            services.Configure<MvcOptions>(options =>
            {
                // Since both XmlSerializer and DataContractSerializer based formatters
                // have supported media types of 'application/xml' and 'text/xml',  it
                // would be difficult for a test to choose a particular formatter based on
                // request information (Ex: Accept header).
                // We'll configure the ones on MvcOptions to use a distinct set of content types.

                XmlSerializerInputFormatter xmlSerializerInputFormatter = null;
                XmlSerializerOutputFormatter xmlSerializerOutputFormatter = null;
                XmlDataContractSerializerInputFormatter dcsInputFormatter = null;
                XmlDataContractSerializerOutputFormatter dcsOutputFormatter = null;

                for (var i = options.InputFormatters.Count - 1; i >= 0; i--)
                {
                    switch (options.InputFormatters[i])
                    {
                        case XmlSerializerInputFormatter formatter:
                            xmlSerializerInputFormatter = formatter;
                            break;

                        case XmlDataContractSerializerInputFormatter formatter:
                            dcsInputFormatter = formatter;
                            break;

                        default:
                            options.InputFormatters.RemoveAt(i);
                            break;
                    }
                }

                for (var i = options.OutputFormatters.Count - 1; i >= 0; i--)
                {
                    switch (options.OutputFormatters[i])
                    {
                        case XmlSerializerOutputFormatter formatter:
                            xmlSerializerOutputFormatter = formatter;
                            break;

                        case XmlDataContractSerializerOutputFormatter formatter:
                            dcsOutputFormatter = formatter;
                            break;

                        default:
                            options.OutputFormatters.RemoveAt(i);
                            break;
                    }
                }

                xmlSerializerInputFormatter.SupportedMediaTypes.Clear();
                xmlSerializerInputFormatter.SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/xml-xmlser"));
                xmlSerializerInputFormatter.SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/xml-xmlser"));
                xmlSerializerInputFormatter.SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/problem+xml"));

                xmlSerializerOutputFormatter.SupportedMediaTypes.Clear();
                xmlSerializerOutputFormatter.SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/xml-xmlser"));
                xmlSerializerOutputFormatter.SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/xml-xmlser"));
                xmlSerializerOutputFormatter.SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/problem+xml"));

                dcsInputFormatter.SupportedMediaTypes.Clear();
                dcsInputFormatter.SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/xml-dcs"));
                dcsInputFormatter.SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/xml-dcs"));
                dcsInputFormatter.SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/problem+xml"));

                dcsOutputFormatter.SupportedMediaTypes.Clear();
                dcsOutputFormatter.SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/xml-dcs"));
                dcsOutputFormatter.SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/xml-dcs"));
                dcsOutputFormatter.SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/problem+xml"));

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
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute();
            });
        }

        public static void Main(string[] args)
        {
            var host = CreateWebHostBuilder(args)
                .Build();

            host.Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            new WebHostBuilder()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<Startup>()
                .UseKestrel()
                .UseIISIntegration();
    }
}

