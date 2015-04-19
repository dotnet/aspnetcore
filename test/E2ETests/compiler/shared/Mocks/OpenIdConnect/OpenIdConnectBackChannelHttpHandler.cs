#if TESTING
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MusicStore.Mocks.OpenIdConnect
{
    internal class OpenIdConnectBackChannelHttpHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage();

            var basePath = Path.GetFullPath(Path.Combine(
                    Directory.GetCurrentDirectory(), "..", "..",
                    "test", "E2ETests", "compiler", "shared", "Mocks",
                    "OpenIdConnect"));

            if (request.RequestUri.AbsoluteUri == "https://login.windows.net/[tenantName].onmicrosoft.com/.well-known/openid-configuration")
            {
                response.Content = new StringContent(File.ReadAllText(Path.Combine(basePath, "openid-configuration.json")));
            }
            else if (request.RequestUri.AbsoluteUri == "https://login.windows.net/common/discovery/keys")
            {
                response.Content = new StringContent(File.ReadAllText(Path.Combine(basePath, "keys.json")));
            }

            return Task.FromResult(response);
        }
    }
} 
#endif