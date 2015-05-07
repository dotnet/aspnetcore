#if TESTING
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.WebUtilities;

namespace MusicStore.Mocks.Google
{
    /// <summary>
    /// Summary description for GoogleMockBackChannelHttpHandler
    /// </summary>
    public class GoogleMockBackChannelHttpHandler : HttpMessageHandler
    {
        protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage();

            if (request.RequestUri.AbsoluteUri.StartsWith("https://accounts.google.com/o/oauth2/token"))
            {
                var formData = new FormCollection(await FormReader.ReadFormAsync(await request.Content.ReadAsStreamAsync()));
                if (formData["grant_type"] == "authorization_code")
                {
                    if (formData["code"] == "ValidCode")
                    {
                        if (formData["redirect_uri"] != null && formData["redirect_uri"].EndsWith("signin-google") &&
                           formData["client_id"] == "[ClientId]" && formData["client_secret"] == "[ClientSecret]")
                        {
                            response.Content = new StringContent("{\"access_token\":\"ValidAccessToken\",\"refresh_token\":\"ValidRefreshToken\",\"token_type\":\"Bearer\",\"expires_in\":\"1200\",\"id_token\":\"Token\"}", Encoding.UTF8, "application/json");
                        }
                    }
                }
            }
            else if (request.RequestUri.AbsoluteUri.StartsWith("https://www.googleapis.com/plus/v1/people/me"))
            {
                if (request.Headers.Authorization.Parameter == "ValidAccessToken")
                {
                    response.Content = new StringContent("{ \"kind\": \"plus#person\",\n \"etag\": \"\\\"YFr-hUROXQN7IOa3dUHg9dQ8eq0/2hY18HdHEP8NLykSTVEiAhkKsBE\\\"\",\n \"gender\": \"male\",\n \"emails\": [\n  {\n   \"value\": \"AspnetvnextTest@gmail.com\",\n   \"type\": \"account\"\n  }\n ],\n \"objectType\": \"person\",\n \"id\": \"106790274378320830963\",\n \"displayName\": \"AspnetvnextTest AspnetvnextTest\",\n \"name\": {\n  \"familyName\": \"AspnetvnextTest\",\n  \"givenName\": \"FirstName\"\n },\n \"url\": \"https://plus.google.com/106790274378320830963\",\n \"image\": {\n  \"url\": \"https://lh3.googleusercontent.com/-XdUIqdMkCWA/AAAAAAAAAAI/AAAAAAAAAAA/4252rscbv5M/photo.jpg?sz=50\"\n },\n \"isPlusUser\": true,\n \"language\": \"en\",\n \"circledByCount\": 0,\n \"verified\": false\n}\n", Encoding.UTF8, "application/json");
                }
                else
                {
                    response.Content = new StringContent("{\"error\":{\"message\":\"Invalid OAuth access token.\",\"type\":\"OAuthException\",\"code\":190}}");
                }
            }

            return response;
        }
    }
} 
#endif