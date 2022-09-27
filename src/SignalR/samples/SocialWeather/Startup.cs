// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using SocialWeather.Json;
using SocialWeather.Pipe;
using SocialWeather.Protobuf;

namespace SocialWeather;

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

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseFileServer();

        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapConnectionHandler<SocialWeatherConnectionHandler>("/weather");
        });

        var formatterResolver = app.ApplicationServices.GetRequiredService<FormatterResolver>();
        formatterResolver.AddFormatter<WeatherReport, JsonStreamFormatter<WeatherReport>>("json");
        formatterResolver.AddFormatter<WeatherReport, ProtobufWeatherStreamFormatter>("protobuf");
        formatterResolver.AddFormatter<WeatherReport, PipeWeatherStreamFormatter>("pipe");
    }
}
