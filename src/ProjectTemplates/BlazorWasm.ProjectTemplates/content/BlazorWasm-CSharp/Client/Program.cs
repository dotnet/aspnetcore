using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using Microsoft.AspNetCore.Blazor.Hosting;
using Microsoft.Extensions.DependencyInjection;
#if (!NoAuth)
using Microsoft.AspNetCore.Components.Authorization;
#endif

#if (Hosted)
namespace BlazorWasm_CSharp.Client
#else
namespace BlazorWasm_CSharp
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
            builder.Services.AddOptions();
            builder.Services.AddAuthorizationCore();
            builder.Services.AddSingleton<AuthenticationStateProvider, HostAuthenticationStateProvider>();
#endif

            await builder.Build().RunAsync();
        }
    }
}
