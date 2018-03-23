// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SocialWeather.Json;
using SocialWeather.Pipe;
using SocialWeather.Protobuf;

namespace SocialWeather
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddConnections();
            services.AddTransient<PersistentConnectionLifeTimeManager>();
            services.AddSingleton(typeof(JsonStreamFormatter<>), typeof(JsonStreamFormatter<>));
            services.AddSingleton<PipeWeatherStreamFormatter>();
            services.AddSingleton<ProtobufWeatherStreamFormatter>();
            services.AddSingleton<FormatterResolver>();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseConnections(o => o.MapConnectionHandler<SocialWeatherConnectionHandler>("/weather"));
            app.UseFileServer();

            var formatterResolver = app.ApplicationServices.GetRequiredService<FormatterResolver>();
            formatterResolver.AddFormatter<WeatherReport, JsonStreamFormatter<WeatherReport>>("json");
            formatterResolver.AddFormatter<WeatherReport, ProtobufWeatherStreamFormatter>("protobuf");
            formatterResolver.AddFormatter<WeatherReport, PipeWeatherStreamFormatter>("pipe");
        }
    }
}
