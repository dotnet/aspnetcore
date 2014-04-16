
using Microsoft.AspNet.DependencyInjection;

namespace Microsoft.AspNet.Mvc
{
    public static class ServiceCollectionExtensions
    {
        public static ServiceCollection AddMvc(this ServiceCollection services)
        {
            return services.Add(MvcServices.GetDefaultServices());
        }
    }
}
