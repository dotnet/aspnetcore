#if (!NoAuth)
using Microsoft.AspNetCore.Components.Authorization;
#endif
using Microsoft.AspNetCore.Components.Builder;
using Microsoft.Extensions.DependencyInjection;

#if (Hosted)
namespace BlazorWasm_CSharp.Client
#else
namespace BlazorWasm_CSharp
#endif
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
#if (IndividualLocalAuth)
            services.AddAuthorizationCore();
            services.AddSingleton<AuthenticationStateProvider, HostAuthenticationStateProvider>();
#endif
        }

        public void Configure(IComponentsApplicationBuilder app)
        {
            app.AddComponent<App>("app");
        }
    }
}
