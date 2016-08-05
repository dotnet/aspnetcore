using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;

namespace MusicStore.Mocks.MicrosoftAccount
{
    /// <summary>
    /// Summary description for MicrosoftAccountMockBackChannelHandler
    /// </summary>
    public class MicrosoftAccountMockBackChannelHandler : HttpMessageHandler
    {
        protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage();

            if (request.RequestUri.AbsoluteUri.StartsWith("https://login.microsoftonline.com/common/oauth2/v2.0/token"))
            {
                var formData = new FormCollection(await new FormReader(await request.Content.ReadAsStreamAsync()).ReadFormAsync());
                if (formData["grant_type"] == "authorization_code")
                {
                    if (formData["code"] == "ValidCode")
                    {
                        if (formData["redirect_uri"].Count > 0 && ((string)formData["redirect_uri"]).EndsWith("signin-microsoft") &&
                           formData["client_id"] == "[ClientId]" && formData["client_secret"] == "[ClientSecret]")
                        {
                            response.Content = new StringContent("{\"token_type\":\"bearer\",\"expires_in\":3600,\"scope\":\"https://graph.microsoft.com/user.read\",\"access_token\":\"ValidAccessToken\",\"refresh_token\":\"ValidRefreshToken\",\"authentication_token\":\"ValidAuthenticationToken\"}");
                            return response;
                        }
                    }
                }

                response.StatusCode = (HttpStatusCode)400;
                return response;
            }
            else if (request.RequestUri.AbsoluteUri.StartsWith("https://graph.microsoft.com/v1.0/me"))
            {
                if (request.Headers.Authorization.Parameter == "ValidAccessToken")
                {
                    response.Content = new StringContent("{\r   \"id\": \"fccf9a24999f4f4f\", \r   \"displayName\": \"AspnetvnextTest AspnetvnextTest\", \r   \"givenName\": \"AspnetvnextTest\", \r   \"surname\": \"AspnetvnextTest\", \r   \"link\": \"https://profile.live.com/\", \r   \"gender\": null, \r   \"locale\": \"en_US\", \r   \"updated_time\": \"2013-08-27T22:18:14+0000\"\r}");
                }
                else
                {
                    response.Content = new StringContent("{\r   \"error\": {\r      \"code\": \"request_token_invalid\", \r      \"message\": \"The access token isn't valid.\"\r   }\r}", Encoding.UTF8, "text/javascript");
                }
                return response;
            }

            throw new NotImplementedException(request.RequestUri.AbsoluteUri);
        }
    }
}
