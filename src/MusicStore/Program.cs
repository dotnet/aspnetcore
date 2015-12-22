using Microsoft.AspNet.Hosting;

namespace MusicStore
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var application = new WebApplicationBuilder()
                .UseConfiguration(WebApplicationConfiguration.GetDefault(args))
                .UseStartup("MusicStore")
                .Build();

            application.Run();
        }
    }
}
