using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;

namespace MusicStore.Mocks.Twitter
{
    /// <summary>
    /// Summary description for TwitterMockBackChannelHttpHandler
    /// </summary>
    public class TwitterMockBackChannelHttpHandler : HttpMessageHandler
    {
        private static bool _requestTokenEndpointInvoked = false;

        protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage();

            if (request.RequestUri.AbsoluteUri.StartsWith("https://api.twitter.com/oauth/access_token"))
            {
                var formData = new FormCollection(await new FormReader(await request.Content.ReadAsStreamAsync()).ReadFormAsync());
                if (formData["oauth_verifier"] == "valid_oauth_verifier")
                {
                    if (_requestTokenEndpointInvoked)
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
                    return response;
                }
                response.StatusCode = (HttpStatusCode)400;
                return response;
            }
            else if (request.RequestUri.AbsoluteUri.StartsWith("https://api.twitter.com/oauth/request_token"))
            {
                var response_Form_data = new List<KeyValuePair<string, string>>()
                {
                    new KeyValuePair<string, string>("oauth_callback_confirmed", "true"),
                    new KeyValuePair<string, string>("oauth_token", "valid_oauth_token"),
                    new KeyValuePair<string, string>("oauth_token_secret", "valid_oauth_token_secret")
                };

                _requestTokenEndpointInvoked = true;
                response.Content = new FormUrlEncodedContent(response_Form_data);
                return response;
            }

            throw new NotImplementedException(request.RequestUri.AbsoluteUri);
        }
    }
}
