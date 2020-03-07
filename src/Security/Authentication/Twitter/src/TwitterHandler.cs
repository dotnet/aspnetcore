// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Authentication.Twitter
{
    public class TwitterHandler : RemoteAuthenticationHandler<TwitterOptions>
    {
        private HttpClient Backchannel => Options.Backchannel;

        /// <summary>
        /// The handler calls methods on the events which give the application control at certain points where processing is occurring.
        /// If it is not provided a default instance is supplied which does nothing when the methods are called.
        /// </summary>
        protected new TwitterEvents Events
        {
            get { return (TwitterEvents)base.Events; }
            set { base.Events = value; }
        }

        public TwitterHandler(IOptionsMonitor<TwitterOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
            : base(options, logger, encoder, clock)
        { }

        protected override Task<object> CreateEventsAsync() => Task.FromResult<object>(new TwitterEvents());

        protected override async Task<HandleRequestResult> HandleRemoteAuthenticateAsync()
        {
            var query = Request.Query;
            var protectedRequestToken = Request.Cookies[Options.StateCookie.Name];

            var requestToken = Options.StateDataFormat.Unprotect(protectedRequestToken);

            if (requestToken == null)
            {
                return HandleRequestResult.Fail("Invalid state cookie.");
            }

            var properties = requestToken.Properties;

            var denied = query["denied"];
            if (!StringValues.IsNullOrEmpty(denied))
            {
                // Note: denied errors are special protocol errors indicating the user didn't
                // approve the authorization demand requested by the remote authorization server.
                // Since it's a frequent scenario (that is not caused by incorrect configuration),
                // denied errors are handled differently using HandleAccessDeniedErrorAsync().
                var result = await HandleAccessDeniedErrorAsync(properties);
                return !result.None ? result
                    : HandleRequestResult.Fail("Access was denied by the resource owner or by the remote server.", properties);
            }

            var returnedToken = query["oauth_token"];
            if (StringValues.IsNullOrEmpty(returnedToken))
            {
                return HandleRequestResult.Fail("Missing oauth_token", properties);
            }

            if (!string.Equals(returnedToken, requestToken.Token, StringComparison.Ordinal))
            {
                return HandleRequestResult.Fail("Unmatched token", properties);
            }

            var oauthVerifier = query["oauth_verifier"];
            if (StringValues.IsNullOrEmpty(oauthVerifier))
            {
                return HandleRequestResult.Fail("Missing or blank oauth_verifier", properties);
            }

            var cookieOptions = Options.StateCookie.Build(Context, Clock.UtcNow);

            Response.Cookies.Delete(Options.StateCookie.Name, cookieOptions);

            var accessToken = await ObtainAccessTokenAsync(requestToken, oauthVerifier);

            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, accessToken.UserId, ClaimValueTypes.String, ClaimsIssuer),
                new Claim(ClaimTypes.Name, accessToken.ScreenName, ClaimValueTypes.String, ClaimsIssuer),
                new Claim("urn:twitter:userid", accessToken.UserId, ClaimValueTypes.String, ClaimsIssuer),
                new Claim("urn:twitter:screenname", accessToken.ScreenName, ClaimValueTypes.String, ClaimsIssuer)
            },
            ClaimsIssuer);

            JsonDocument user;
            if (Options.RetrieveUserDetails)
            {
                user = await RetrieveUserDetailsAsync(accessToken, identity);
            }
            else
            {
                user = JsonDocument.Parse("{}");
            }

            using (user)
            {
                if (Options.SaveTokens)
                {
                    properties.StoreTokens(new[] {
                    new AuthenticationToken { Name = "access_token", Value = accessToken.Token },
                    new AuthenticationToken { Name = "access_token_secret", Value = accessToken.TokenSecret }
                    });
                }

                var ticket = await CreateTicketAsync(identity, properties, accessToken, user.RootElement);
                return HandleRequestResult.Success(ticket);
            }
        }

        protected virtual async Task<AuthenticationTicket> CreateTicketAsync(
            ClaimsIdentity identity, AuthenticationProperties properties, AccessToken token, JsonElement user)
        {
            foreach (var action in Options.ClaimActions)
            {
                action.Run(user, identity, ClaimsIssuer);
            }

            var context = new TwitterCreatingTicketContext(Context, Scheme, Options, new ClaimsPrincipal(identity), properties, token.UserId, token.ScreenName, token.Token, token.TokenSecret, user);
            await Events.CreatingTicket(context);

            return new AuthenticationTicket(context.Principal, context.Properties, Scheme.Name);
        }

        protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            if (string.IsNullOrEmpty(properties.RedirectUri))
            {
                properties.RedirectUri = OriginalPathBase + OriginalPath + Request.QueryString;
            }

            // If CallbackConfirmed is false, this will throw
            var requestToken = await ObtainRequestTokenAsync(BuildRedirectUri(Options.CallbackPath), properties);
            var twitterAuthenticationEndpoint = TwitterDefaults.AuthenticationEndpoint + requestToken.Token;

            var cookieOptions = Options.StateCookie.Build(Context, Clock.UtcNow);

            Response.Cookies.Append(Options.StateCookie.Name, Options.StateDataFormat.Protect(requestToken), cookieOptions);

            var redirectContext = new RedirectContext<TwitterOptions>(Context, Scheme, Options, properties, twitterAuthenticationEndpoint);
            await Events.RedirectToAuthorizationEndpoint(redirectContext);
        }

        private async Task<HttpResponseMessage> ExecuteRequestAsync(string url, HttpMethod httpMethod, RequestToken accessToken = null, Dictionary<string, string> extraOAuthPairs = null, Dictionary<string, string> queryParameters = null, Dictionary<string, string> formData = null)
        {
            var authorizationParts = new SortedDictionary<string, string>(extraOAuthPairs ?? new Dictionary<string, string>())
            {
                { "oauth_consumer_key", Options.ConsumerKey },
                { "oauth_nonce", Guid.NewGuid().ToString("N") },
                { "oauth_signature_method", "HMAC-SHA1" },
                { "oauth_timestamp", GenerateTimeStamp() },
                { "oauth_version", "1.0" }
            };

            if (accessToken != null)
            {
                authorizationParts.Add("oauth_token", accessToken.Token);
            }

            var signatureParts = new SortedDictionary<string, string>(authorizationParts);
            if (queryParameters != null)
            {
                foreach (var queryParameter in queryParameters)
                {
                    signatureParts.Add(queryParameter.Key, queryParameter.Value);
                }
            }
            if (formData != null)
            {
                foreach (var formItem in formData)
                {
                    signatureParts.Add(formItem.Key, formItem.Value);
                }
            }

            var parameterBuilder = new StringBuilder();
            foreach (var signaturePart in signatureParts)
            {
                parameterBuilder.AppendFormat("{0}={1}&", Uri.EscapeDataString(signaturePart.Key), Uri.EscapeDataString(signaturePart.Value));
            }
            parameterBuilder.Length--;
            var parameterString = parameterBuilder.ToString();

            var canonicalizedRequestBuilder = new StringBuilder();
            canonicalizedRequestBuilder.Append(httpMethod.Method);
            canonicalizedRequestBuilder.Append("&");
            canonicalizedRequestBuilder.Append(Uri.EscapeDataString(url));
            canonicalizedRequestBuilder.Append("&");
            canonicalizedRequestBuilder.Append(Uri.EscapeDataString(parameterString));

            var signature = ComputeSignature(Options.ConsumerSecret, accessToken?.TokenSecret, canonicalizedRequestBuilder.ToString());
            authorizationParts.Add("oauth_signature", signature);

            var queryString = "";
            if (queryParameters != null)
            {
                var queryStringBuilder = new StringBuilder("?");
                foreach (var queryParam in queryParameters)
                {
                    queryStringBuilder.AppendFormat("{0}={1}&", queryParam.Key, queryParam.Value);
                }
                queryStringBuilder.Length--;
                queryString = queryStringBuilder.ToString();
            }

            var authorizationHeaderBuilder = new StringBuilder();
            authorizationHeaderBuilder.Append("OAuth ");
            foreach (var authorizationPart in authorizationParts)
            {
                authorizationHeaderBuilder.AppendFormat("{0}=\"{1}\",", authorizationPart.Key, Uri.EscapeDataString(authorizationPart.Value));
            }
            authorizationHeaderBuilder.Length--;

            var request = new HttpRequestMessage(httpMethod, url + queryString);
            request.Headers.Add("Authorization", authorizationHeaderBuilder.ToString());

            if (formData != null)
            {
                request.Content = new FormUrlEncodedContent(formData);
            }

            return await Backchannel.SendAsync(request, Context.RequestAborted);
        }

        private async Task<RequestToken> ObtainRequestTokenAsync(string callBackUri, AuthenticationProperties properties)
        {
            Logger.ObtainRequestToken();

            var response = await ExecuteRequestAsync(TwitterDefaults.RequestTokenEndpoint, HttpMethod.Post, extraOAuthPairs: new Dictionary<string, string>() { { "oauth_callback", callBackUri } });
            response.EnsureSuccessStatusCode();
            var responseText = await response.Content.ReadAsStringAsync();

            var responseParameters = new FormCollection(new FormReader(responseText).ReadForm());
            if (!string.Equals(responseParameters["oauth_callback_confirmed"], "true", StringComparison.Ordinal))
            {
                throw new Exception("Twitter oauth_callback_confirmed is not true.");
            }

            return new RequestToken { Token = Uri.UnescapeDataString(responseParameters["oauth_token"]), TokenSecret = Uri.UnescapeDataString(responseParameters["oauth_token_secret"]), CallbackConfirmed = true, Properties = properties };
        }

        private async Task<AccessToken> ObtainAccessTokenAsync(RequestToken token, string verifier)
        {
            // https://dev.twitter.com/docs/api/1/post/oauth/access_token

            Logger.ObtainAccessToken();

            var formPost = new Dictionary<string, string> { { "oauth_verifier", verifier } };
            var response = await ExecuteRequestAsync(TwitterDefaults.AccessTokenEndpoint, HttpMethod.Post, token, formData: formPost);

            if (!response.IsSuccessStatusCode)
            {
                Logger.LogError("AccessToken request failed with a status code of " + response.StatusCode);
                response.EnsureSuccessStatusCode(); // throw
            }

            var responseText = await response.Content.ReadAsStringAsync();
            var responseParameters = new FormCollection(new FormReader(responseText).ReadForm());

            return new AccessToken
            {
                Token = Uri.UnescapeDataString(responseParameters["oauth_token"]),
                TokenSecret = Uri.UnescapeDataString(responseParameters["oauth_token_secret"]),
                UserId = Uri.UnescapeDataString(responseParameters["user_id"]),
                ScreenName = Uri.UnescapeDataString(responseParameters["screen_name"])
            };
        }

        // https://dev.twitter.com/rest/reference/get/account/verify_credentials
        private async Task<JsonDocument> RetrieveUserDetailsAsync(AccessToken accessToken, ClaimsIdentity identity)
        {
            Logger.RetrieveUserDetails();

            var response = await ExecuteRequestAsync("https://api.twitter.com/1.1/account/verify_credentials.json", HttpMethod.Get, accessToken, queryParameters: new Dictionary<string, string>() { { "include_email", "true" } });

            if (!response.IsSuccessStatusCode)
            {
                Logger.LogError("Email request failed with a status code of " + response.StatusCode);
                response.EnsureSuccessStatusCode(); // throw
            }
            var responseText = await response.Content.ReadAsStringAsync();

            var result = JsonDocument.Parse(responseText);

            return result;
        }

        private string GenerateTimeStamp()
        {
            var secondsSinceUnixEpocStart = Clock.UtcNow - DateTimeOffset.UnixEpoch;
            return Convert.ToInt64(secondsSinceUnixEpocStart.TotalSeconds).ToString(CultureInfo.InvariantCulture);
        }

        private string ComputeSignature(string consumerSecret, string tokenSecret, string signatureData)
        {
            using (var algorithm = new HMACSHA1())
            {
                algorithm.Key = Encoding.ASCII.GetBytes(
                    string.Format(CultureInfo.InvariantCulture,
                        "{0}&{1}",
                        Uri.EscapeDataString(consumerSecret),
                        string.IsNullOrEmpty(tokenSecret) ? string.Empty : Uri.EscapeDataString(tokenSecret)));
                var hash = algorithm.ComputeHash(Encoding.ASCII.GetBytes(signatureData));
                return Convert.ToBase64String(hash);
            }
        }
    }
}
