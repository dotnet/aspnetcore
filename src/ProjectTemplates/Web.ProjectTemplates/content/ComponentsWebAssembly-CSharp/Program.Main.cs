using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace ComponentsWebAssembly_CSharp;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        builder.RootComponents.Add<App>("#app");
        builder.RootComponents.Add<HeadOutlet>("head::after");

        builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
        #if(!NoAuth)

        #endif
        #if (IndividualLocalAuth)
        builder.Services.AddOidcAuthentication(options =>
        {
            #if(MissingAuthority)
            // Configure your authentication provider options here.
            // For more information, see https://aka.ms/blazor-standalone-auth
            #endif
            builder.Configuration.Bind("Local", options.ProviderOptions);
        });
        #endif
        #if (IndividualB2CAuth)
        builder.Services.AddMsalAuthentication(options =>
        {
            builder.Configuration.Bind("AzureAdB2C", options.ProviderOptions.Authentication);
        });
        #endif
        #if(OrganizationalAuth)
        builder.Services.AddMsalAuthentication(options =>
        {
            builder.Configuration.Bind("AzureAd", options.ProviderOptions.Authentication);
        });
        #endif

        await builder.Build().RunAsync();
    }
}
