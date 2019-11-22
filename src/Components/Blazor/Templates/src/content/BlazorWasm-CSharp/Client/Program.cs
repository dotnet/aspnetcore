using Microsoft.AspNetCore.Blazor.Hosting;

#if (Hosted)
namespace BlazorWasm_CSharp.Client
#else
namespace BlazorWasm_CSharp
#endif
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IWebAssemblyHostBuilder CreateHostBuilder(string[] args) =>
            BlazorWebAssemblyHost.CreateDefaultBuilder()
                .UseBlazorStartup<Startup>();
    }
}
