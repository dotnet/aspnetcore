#if TESTING
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.WebUtilities;
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
            var queryParameters = new ReadableStringCollection(QueryHelpers.ParseQuery(request.RequestUri.Query));

            if (request.RequestUri.AbsoluteUri.StartsWith("https://graph.facebook.com/v2.2/oauth/access_token"))
            {
                if (queryParameters["grant_type"] == "authorization_code")
                {
                    if (queryParameters["code"] == "ValidCode")
                    {
                        Helpers.ThrowIfConditionFailed(() => queryParameters["redirect_uri"].EndsWith("signin-facebook"), "Redirect URI is not ending with /signin-facebook");
                        Helpers.ThrowIfConditionFailed(() => queryParameters["client_id"] == "[AppId]", "Invalid client Id received");
                        Helpers.ThrowIfConditionFailed(() => queryParameters["client_secret"] == "[AppSecret]", "Invalid client secret received");
                        response.Content = new StringContent("access_token=ValidAccessToken&expires=100");
                    }
                }
            }
            else if (request.RequestUri.AbsoluteUri.StartsWith("https://graph.facebook.com/v2.2/me"))
            {
                Helpers.ThrowIfConditionFailed(() => queryParameters["appsecret_proof"] != null, "appsecret_proof is null");
                if (queryParameters["access_token"] == "ValidAccessToken")
                {
                    response.Content = new StringContent("{\"id\":\"Id\",\"name\":\"AspnetvnextTest AspnetvnextTest\",\"first_name\":\"AspnetvnextTest\",\"last_name\":\"AspnetvnextTest\",\"link\":\"https:\\/\\/www.facebook.com\\/myLink\",\"username\":\"AspnetvnextTest.AspnetvnextTest.7\",\"gender\":\"male\",\"email\":\"AspnetvnextTest\\u0040test.com\",\"timezone\":-7,\"locale\":\"en_US\",\"verified\":true,\"updated_time\":\"2013-08-06T20:38:48+0000\",\"CertValidatorInvoked\":\"ValidAccessToken\"}");
                }
                else
                {
                    response.Content = new StringContent("{\"error\":{\"message\":\"Invalid OAuth access token.\",\"type\":\"OAuthException\",\"code\":190}}");
                }
            }

            return await Task.FromResult(response);
        }
    }
} 
#endif