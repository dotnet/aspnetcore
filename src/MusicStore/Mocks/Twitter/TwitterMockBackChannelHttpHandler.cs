using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.WebUtilities;
using System.Collections.Generic;
using System.Net;

namespace MusicStore.Mocks.Twitter
{
    /// <summary>
    /// Summary description for TwitterMockBackChannelHttpHandler
    /// </summary>
    public class TwitterMockBackChannelHttpHandler : HttpMessageHandler
    {
        private static bool RequestTokenEndpointInvoked = false;

        protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage();
            Console.WriteLine(request.RequestUri.AbsoluteUri);

            if (request.RequestUri.AbsoluteUri.StartsWith("https://api.twitter.com/oauth/access_token"))
            {
                var formData = FormHelpers.ParseForm(await request.Content.ReadAsStringAsync());
                if (formData["oauth_verifier"] == "valid_oauth_verifier")
                {
                    if (RequestTokenEndpointInvoked)
                    {
                        var response_Form_data = new List<KeyValuePair<string, string>>()
                            {
                                new KeyValuePair<string, string>("oauth_token", "valid_oauth_token"),
                                new KeyValuePair<string, string>("oauth_token_secret", "valid_oauth_token_secret"),
                                new KeyValuePair<string, string>("user_id", "valid_user_id"),
                                new KeyValuePair<string, string>("screen_name", "valid_screen_name"),
                            };

                        response.Content = new FormUrlEncodedContent(response_Form_data);
                    }
                    else
                    {
                        response.StatusCode = HttpStatusCode.InternalServerError;
                        response.Content = new StringContent("RequestTokenEndpoint is not invoked");
                    }
                }
            }
            else if (request.RequestUri.AbsoluteUri.StartsWith("https://api.twitter.com/oauth/request_token"))
            {
                var response_Form_data = new List<KeyValuePair<string, string>>()
                {
                    new KeyValuePair<string, string>("oauth_callback_confirmed", "true"),
                    new KeyValuePair<string, string>("oauth_token", "valid_oauth_token"),
                    new KeyValuePair<string, string>("oauth_token_secret", "valid_oauth_token_secret")
                };

                RequestTokenEndpointInvoked = true;
                response.Content = new FormUrlEncodedContent(response_Form_data);
            }

            return response;
        }
    }
}