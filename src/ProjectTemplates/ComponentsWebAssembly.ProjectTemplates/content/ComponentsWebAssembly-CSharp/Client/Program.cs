using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;

#if (Hosted)
namespace ComponentsWebAssembly_CSharp.Client
#else
namespace ComponentsWebAssembly_CSharp
#endif
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("app");

            // use builder.Services to configure application services.
#if (IndividualLocalAuth)
    #if (Hosted)
            builder.Services.AddApiAuthorization();
    #else
            builder.Services.AddOidcAuthentication(options =>
            {
                options.ProviderOptions.Authority = "https://login.microsoftonline.com/";
                options.ProviderOptions.ClientId = "33333333-3333-3333-33333333333333333";
            });
    #endif
#endif
#if (IndividualB2CAuth)
            builder.Services.AddMsalSpaAuthentication(options =>
            {
                var authentication = options.ProviderOptions.Authentication;
                authentication.Authority = "https:////aadB2CInstance.b2clogin.com/qualified.domain.name/MySignUpSignInPolicyId";
                authentication.ClientId = "33333333-3333-3333-33333333333333333";
                authentication.ValidateAuthority = false;
#if (Hosted)
                options.ProviderOptions.DefaultAccessTokenScopes.Add("https://qualified.domain.name/api.id.uri/api-scope");
#endif
            });
#endif
#if(OrganizationalAuth)
            builder.Services.AddMsalSpaAuthentication(options =>
            {
                var authentication = options.ProviderOptions.Authentication;
                authentication.Authority = "https://login.microsoftonline.com/22222222-2222-2222-2222-222222222222";
                authentication.ClientId = "33333333-3333-3333-33333333333333333";
#if (Hosted)
                options.ProviderOptions.DefaultAccessTokenScopes.Add("api://api.id.uri/api-scope");
#endif
            });
#endif

            await builder.Build().RunAsync();
        }
    }
}
