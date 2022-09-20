using Microsoft.AspNetCore.Components.Web;
#if (!NoAuth && Hosted)
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
#endif
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
#if (Hosted)
using ComponentsWebAssembly_CSharp.Client;
#else
using ComponentsWebAssembly_CSharp;
#endif

#if (Hosted)
namespace ComponentsWebAssembly_CSharp.Client;
#else
namespace ComponentsWebAssembly_CSharp;
#endif

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        builder.RootComponents.Add<App>("#app");
        builder.RootComponents.Add<HeadOutlet>("head::after");

        #if (!Hosted || NoAuth)
        builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
        #else
        builder.Services.AddHttpClient("ComponentsWebAssembly_CSharp.ServerAPI", client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress))
            .AddHttpMessageHandler<BaseAddressAuthorizationMessageHandler>();

        // Supply HttpClient instances that include access tokens when making requests to the server project
        builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("ComponentsWebAssembly_CSharp.ServerAPI"));
        #endif
        #if(!NoAuth)

        #endif
        #if (IndividualLocalAuth)
            #if (Hosted)
        builder.Services.AddApiAuthorization();
            #else
        builder.Services.AddOidcAuthentication(options =>
        {
            #if(MissingAuthority)
            // Configure your authentication provider options here.
            // For more information, see https://aka.ms/blazor-standalone-auth
            #endif
            builder.Configuration.Bind("Local", options.ProviderOptions);
        });
            #endif
        #endif
        #if (IndividualB2CAuth)
        builder.Services.AddMsalAuthentication(options =>
        {
            builder.Configuration.Bind("AzureAdB2C", options.ProviderOptions.Authentication);
        #if (Hosted)
            options.ProviderOptions.DefaultAccessTokenScopes.Add("https://qualified.domain.name/api.id.uri/api-scope");
        #endif
        });
        #endif
        #if(OrganizationalAuth)
        builder.Services.AddMsalAuthentication(options =>
        {
            builder.Configuration.Bind("AzureAd", options.ProviderOptions.Authentication);
        #if (Hosted)
            options.ProviderOptions.DefaultAccessTokenScopes.Add("api://api.id.uri/api-scope");
        #endif
        });
        #endif

        await builder.Build().RunAsync();
    }
}
