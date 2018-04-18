using Microsoft.AspNetCore.Hosting.Server.Features;
using System.Linq;

namespace Microsoft.AspNetCore.Hosting
{
    public static class IWebHostExtensions
    {
        public static string GetAddress(this IWebHost host)
        {
            return host.ServerFeatures.Get<IServerAddressesFeature>().Addresses.First();
        }
    }
}
