using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace DeveloperExceptionPageSample
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app)
        {
            app.UseDeveloperExceptionPage();
            app.Run(context =>
            {
                throw new Exception(string.Concat(
                    "Demonstration exception. The list:", "\r\n",
                    "New Line 1", "\n",
                    "New Line 2", Environment.NewLine,
                    "New Line 3"));
            });
        }

        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .UseKestrel()
                .UseIISIntegration()
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }
    }
}
