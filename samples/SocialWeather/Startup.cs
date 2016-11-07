using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SocialWeather.Pipe;
using SocialWeather.Protobuf;

namespace SocialWeather
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRouting();
            services.AddSingleton<SocialWeatherEndPoint>();
            services.AddTransient<PersistentConnectionLifeTimeManager>();
            services.AddSingleton(typeof(JsonStreamFormatter<>), typeof(JsonStreamFormatter<>));
            services.AddSingleton<PipeWeatherStreamFormatter>();
            services.AddSingleton<ProtobufWeatherStreamFormatter>();
            services.AddSingleton<FormatterResolver>();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseSockets(o => { o.MapEndpoint<SocialWeatherEndPoint>("/weather"); });
            app.UseStaticFiles();

            var formatterResolver = app.ApplicationServices.GetRequiredService<FormatterResolver>();
            formatterResolver.AddFormatter<WeatherReport, JsonStreamFormatter<WeatherReport>>("json");
            formatterResolver.AddFormatter<WeatherReport, ProtobufWeatherStreamFormatter>("protobuf");
            formatterResolver.AddFormatter<WeatherReport, PipeWeatherStreamFormatter>("pipe");
        }
    }
}
