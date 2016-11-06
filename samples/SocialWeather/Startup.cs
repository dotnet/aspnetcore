using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SocialWeather
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRouting();
            services.AddSingleton<SocialWeatherEndPoint>();
            services.AddTransient<PersistentConnectionLifeTimeManager>();
            services.AddSingleton(typeof(JsonStreamFormatter<>), typeof(JsonStreamFormatter<>));
            services.AddSingleton<ProtobufWeatherStreamFormatter>();
            services.AddSingleton<FormatterResolver>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
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

            app.Run(async (context) =>
            {
                await context.Response.WriteAsync("Hello World!");
            });
        }
    }
}
