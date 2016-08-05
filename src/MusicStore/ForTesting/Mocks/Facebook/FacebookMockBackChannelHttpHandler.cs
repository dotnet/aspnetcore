using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.WebUtilities;
using MusicStore.Mocks.Common;

namespace MusicStore.Mocks.Facebook
{
    /// <summary>
    /// Summary description for FacebookMockBackChannelHttpHandler
    /// </summary>
    public class FacebookMockBackChannelHttpHandler : HttpMessageHandler
    {
        protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage();

            if (request.RequestUri.AbsoluteUri.StartsWith("https://graph.facebook.com/v2.6/oauth/access_token"))
            {
                var formData = new FormCollection(await new FormReader(await request.Content.ReadAsStreamAsync()).ReadFormAsync());
                if (formData["grant_type"] == "authorization_code")
                {
                    if (formData["code"] == "ValidCode")
                    {
                        Helpers.ThrowIfConditionFailed(() => ((string)formData["redirect_uri"]).EndsWith("signin-facebook"), "Redirect URI is not ending with /signin-facebook");
                        Helpers.ThrowIfConditionFailed(() => formData["client_id"] == "[AppId]", "Invalid client Id received");
                        Helpers.ThrowIfConditionFailed(() => formData["client_secret"] == "[AppSecret]", "Invalid client secret received");
                        response.Content = new StringContent("{ \"access_token\": \"ValidAccessToken\", \"expires_in\": \"100\" }");
                        return response;
                    }
                    response.StatusCode = (HttpStatusCode)400;
                    return response;
                }
            }
            else if (request.RequestUri.AbsoluteUri.StartsWith("https://graph.facebook.com/v2.6/me"))
            {
                var queryParameters = new QueryCollection(QueryHelpers.ParseQuery(request.RequestUri.Query));
                Helpers.ThrowIfConditionFailed(() => queryParameters["appsecret_proof"].Count > 0, "appsecret_proof is empty");
                if (queryParameters["access_token"] == "ValidAccessToken")
                {
                    response.Content = new StringContent("{\"id\":\"Id\",\"name\":\"AspnetvnextTest AspnetvnextTest\",\"first_name\":\"AspnetvnextTest\",\"last_name\":\"AspnetvnextTest\",\"link\":\"https:\\/\\/www.facebook.com\\/myLink\",\"username\":\"AspnetvnextTest.AspnetvnextTest.7\",\"gender\":\"male\",\"email\":\"AspnetvnextTest\\u0040test.com\",\"timezone\":-7,\"locale\":\"en_US\",\"verified\":true,\"updated_time\":\"2013-08-06T20:38:48+0000\",\"CertValidatorInvoked\":\"ValidAccessToken\"}");
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
