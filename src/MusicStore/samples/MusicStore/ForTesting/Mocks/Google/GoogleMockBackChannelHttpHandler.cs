using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;

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

            if (request.RequestUri.AbsoluteUri.StartsWith("https://www.googleapis.com/oauth2/v4/token"))
            {
                var formData = new FormCollection(await new FormReader(await request.Content.ReadAsStreamAsync()).ReadFormAsync());
                if (formData["grant_type"] == "authorization_code")
                {
                    if (formData["code"] == "ValidCode")
                    {
                        if (formData["redirect_uri"].Count > 0 && ((string)formData["redirect_uri"]).EndsWith("signin-google") &&
                           formData["client_id"] == "[ClientId]" && formData["client_secret"] == "[ClientSecret]")
                        {
                            response.Content = new StringContent("{\"access_token\":\"ValidAccessToken\",\"refresh_token\":\"ValidRefreshToken\",\"token_type\":\"Bearer\",\"expires_in\":\"1200\",\"id_token\":\"Token\"}", Encoding.UTF8, "application/json");
                            return response;
                        }
                    }
                }
                response.StatusCode = (HttpStatusCode)400;
                return response;
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
                return response;
            }

            throw new NotImplementedException(request.RequestUri.AbsoluteUri);
        }
    }
}
