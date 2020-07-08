using Microsoft.AspNetCore.Components.Web.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class HeadServiceCollectionExtensions
    {
        public static void AddHeadManager(this IServiceCollection services)
        {
            services.AddScoped<HeadManager>();
        }
    }
}
