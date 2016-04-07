using Microsoft.AspNetCore.Hosting;

namespace MusicStore
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                // We set the server before default args so that command line arguments can override it.
                // This is used to allow deployers to choose the server for testing.
                .UseKestrel()
                .UseDefaultHostingConfiguration(args)
                .UseIISIntegration()
                .UseStartup("MusicStore")
                .Build();

            host.Run();
        }
    }
}
