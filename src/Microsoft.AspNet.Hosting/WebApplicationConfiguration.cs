using Microsoft.Extensions.Configuration;

namespace Microsoft.AspNet.Hosting
{
    public class WebApplicationConfiguration
    {
        public static IConfiguration GetDefault()
        {
            return GetDefault(args: null);
        }

        public static IConfiguration GetDefault(string[] args)
        {
            // We are adding all environment variables first and then adding the ASPNET_ ones
            // with the prefix removed to unify with the command line and config file formats
            var configBuilder = new ConfigurationBuilder()
                .AddJsonFile(WebApplicationDefaults.HostingJsonFile, optional: true)
                .AddEnvironmentVariables()
                .AddEnvironmentVariables(prefix: WebApplicationDefaults.EnvironmentVariablesPrefix);

            if (args != null)
            {
                configBuilder.AddCommandLine(args);
            }

            return configBuilder.Build();
        }
    }

}
