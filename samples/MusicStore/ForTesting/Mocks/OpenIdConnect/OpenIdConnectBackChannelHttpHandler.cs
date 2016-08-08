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
                Directory.GetCurrentDirectory(), "ForTesting", "Mocks", "OpenIdConnect"));

            if (request.RequestUri.AbsoluteUri == "https://login.windows.net/[tenantName].onmicrosoft.com/.well-known/openid-configuration")
            {
                response.Content = new StringContent(File.ReadAllText(Path.Combine(basePath, "openid-configuration.json")));
            }
            else if (request.RequestUri.AbsoluteUri == "https://login.windows.net/common/discovery/keys")
            {
                response.Content = new StringContent(File.ReadAllText(Path.Combine(basePath, "keys.json")));
            }
            else if (request.RequestUri.AbsoluteUri == "https://login.windows.net/4afbc689-805b-48cf-a24c-d4aa3248a248/oauth2/token")
            {
                response.Content = new StringContent("{\"id_token\": \"id\", \"access_token\": \"access\"}");
            }

            return Task.FromResult(response);
        }
    }
}
