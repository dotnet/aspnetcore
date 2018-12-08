using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace Microsoft.AspNetCore.Authentication.OpenIdConnect
{
    /// <summary>
    /// Use basic authorization header
    /// </summary>
    public class BasicAuthentication : IClientAuthentication
    {
        private readonly bool withEscaping;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="withEscaping">allow to do EscapeData on the clientId and clientSecret</param>
        public BasicAuthentication(bool withEscaping = true)
        {
            this.withEscaping = withEscaping;
        }

        public HttpRequestMessage SetClientAuthentication(HttpRequestMessage message, OpenIdConnectMessage tokenEndpointRequest)
        {
            var idAndSecret = withEscaping ? GetUtf8EscapedIdAndSecret(tokenEndpointRequest) : GetIdAndSecret(tokenEndpointRequest);
            var basicHeader = Convert.ToBase64String(
                   Encoding.UTF8.GetBytes(idAndSecret));
            message.Headers.Authorization = new AuthenticationHeaderValue("Basic", basicHeader);
            tokenEndpointRequest.ClientId = null;
            tokenEndpointRequest.ClientSecret = null;
            return message;
        }

        private string GetUtf8EscapedIdAndSecret(OpenIdConnectMessage tokenEndpointRequest)
        {
            return $"{Uri.EscapeDataString(tokenEndpointRequest.ClientId.ToUTF8())}:{Uri.EscapeDataString(tokenEndpointRequest.ClientSecret.ToUTF8())}";
        }

        private string GetIdAndSecret(OpenIdConnectMessage tokenEndpointRequest)
        {
            return $"{tokenEndpointRequest.ClientId}:{tokenEndpointRequest.ClientSecret}";
        }
    }
}
