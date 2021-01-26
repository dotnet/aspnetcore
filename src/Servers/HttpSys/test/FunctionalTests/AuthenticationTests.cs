// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.HttpSys
{
    public class AuthenticationTests
    {
        private static bool AllowAnoymous = true;
        private static bool DenyAnoymous = false;

        [ConditionalTheory]
        [InlineData(AuthenticationSchemes.None)]
        [InlineData(AuthenticationSchemes.Negotiate)]
        [InlineData(AuthenticationSchemes.NTLM)]
        // [InlineData(AuthenticationSchemes.Digest)]
        [InlineData(AuthenticationSchemes.Basic)]
        [InlineData(AuthenticationSchemes.Negotiate | AuthenticationSchemes.NTLM | /*AuthenticationSchemes.Digest |*/ AuthenticationSchemes.Basic)]
        public async Task AuthTypes_AllowAnonymous_NoChallenge(AuthenticationSchemes authType)
        {
            using (var server = Utilities.CreateDynamicHost(authType, AllowAnoymous, out var address, httpContext =>
            {
                Assert.NotNull(httpContext.User);
                Assert.NotNull(httpContext.User.Identity);
                Assert.False(httpContext.User.Identity.IsAuthenticated);
                return Task.FromResult(0);
            }))
            {
                var response = await SendRequestAsync(address);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Empty(response.Headers.WwwAuthenticate);
            }
        }

        [ConditionalTheory]
        [InlineData(AuthenticationSchemes.Negotiate)]
        [InlineData(AuthenticationSchemes.NTLM)]
        // [InlineData(AuthenticationSchemes.Digest)] // TODO: Not implemented
        [InlineData(AuthenticationSchemes.Basic)]
        public async Task AuthType_RequireAuth_ChallengesAdded(AuthenticationSchemes authType)
        {
            using (var server = Utilities.CreateDynamicHost(authType, DenyAnoymous, out var address, httpContext =>
            {
                throw new NotImplementedException();
            }))
            {
                var response = await SendRequestAsync(address);
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                Assert.Equal(authType.ToString(), response.Headers.WwwAuthenticate.ToString(), StringComparer.OrdinalIgnoreCase);
            }
        }

        [ConditionalTheory]
        [InlineData(AuthenticationSchemes.Negotiate)]
        [InlineData(AuthenticationSchemes.NTLM)]
        // [InlineData(AuthenticationSchemes.Digest)] // TODO: Not implemented
        [InlineData(AuthenticationSchemes.Basic)]
        public async Task AuthType_AllowAnonymousButSpecify401_ChallengesAdded(AuthenticationSchemes authType)
        {
            using (var server = Utilities.CreateDynamicHost(authType, AllowAnoymous, out var address, httpContext =>
            {
                Assert.NotNull(httpContext.User);
                Assert.NotNull(httpContext.User.Identity);
                Assert.False(httpContext.User.Identity.IsAuthenticated);
                httpContext.Response.StatusCode = 401;
                return Task.FromResult(0);
            }))
            {
                var response = await SendRequestAsync(address);
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                Assert.Equal(authType.ToString(), response.Headers.WwwAuthenticate.ToString(), StringComparer.OrdinalIgnoreCase);
            }
        }

        [ConditionalFact]
        public async Task MultipleAuthTypes_AllowAnonymousButSpecify401_ChallengesAdded()
        {
            string address;
            using (Utilities.CreateHttpAuthServer(
                AuthenticationSchemes.Negotiate
                | AuthenticationSchemes.NTLM
                /* | AuthenticationSchemes.Digest TODO: Not implemented */
                | AuthenticationSchemes.Basic,
                true,
                out address,
                httpContext =>
            {
                Assert.NotNull(httpContext.User);
                Assert.NotNull(httpContext.User.Identity);
                Assert.False(httpContext.User.Identity.IsAuthenticated);
                httpContext.Response.StatusCode = 401;
                return Task.FromResult(0);
            }))
            {
                var response = await SendRequestAsync(address);
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                Assert.Equal("Negotiate, NTLM, basic", response.Headers.WwwAuthenticate.ToString(), StringComparer.OrdinalIgnoreCase);
            }
        }

        [ConditionalTheory]
        [InlineData(AuthenticationSchemes.Negotiate)]
        [InlineData(AuthenticationSchemes.NTLM)]
        // [InlineData(AuthenticationSchemes.Digest)] // TODO: Not implemented
        // [InlineData(AuthenticationSchemes.Basic)] // Doesn't work with default creds
        [InlineData(AuthenticationSchemes.Negotiate | AuthenticationSchemes.NTLM | /* AuthenticationSchemes.Digest |*/ AuthenticationSchemes.Basic)]
        public async Task AuthTypes_AllowAnonymousButSpecify401_Success(AuthenticationSchemes authType)
        {
            int requestId = 0;
            using (var server = Utilities.CreateDynamicHost(authType, AllowAnoymous, out var address, httpContext =>
            {
                Assert.NotNull(httpContext.User);
                Assert.NotNull(httpContext.User.Identity);
                if (requestId == 0)
                {
                    Assert.False(httpContext.User.Identity.IsAuthenticated);
                    httpContext.Response.StatusCode = 401;
                }
                else if (requestId == 1)
                {
                    Assert.True(httpContext.User.Identity.IsAuthenticated);
                }
                else
                {
                    throw new NotImplementedException();
                }
                requestId++;
                return Task.FromResult(0);
            }))
            {
                var response = await SendRequestAsync(address, useDefaultCredentials: true);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        [ConditionalTheory]
        [InlineData(AuthenticationSchemes.Negotiate)]
        [InlineData(AuthenticationSchemes.NTLM)]
        // [InlineData(AuthenticationSchemes.Digest)] // TODO: Not implemented
        // [InlineData(AuthenticationSchemes.Basic)] // Doesn't work with default creds
        [InlineData(AuthenticationSchemes.Negotiate | AuthenticationSchemes.NTLM | /* AuthenticationSchemes.Digest |*/ AuthenticationSchemes.Basic)]
        public async Task AuthTypes_RequireAuth_Success(AuthenticationSchemes authType)
        {
            using (var server = Utilities.CreateDynamicHost(authType, DenyAnoymous, out var address, httpContext =>
            {
                Assert.NotNull(httpContext.User);
                Assert.NotNull(httpContext.User.Identity);
                Assert.True(httpContext.User.Identity.IsAuthenticated);
                return Task.FromResult(0);
            }))
            {
                var response = await SendRequestAsync(address, useDefaultCredentials: true);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        // https://github.com/aspnet/Logging/issues/543#issuecomment-321907828
        [ConditionalFact]
        public async Task AuthTypes_AccessUserInOnCompleted_Success()
        {
            var completed = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            string userName = null;
            var authTypes = AuthenticationSchemes.Negotiate | AuthenticationSchemes.NTLM;
            using (var server = Utilities.CreateDynamicHost(authTypes, DenyAnoymous, out var address, httpContext =>
            {
                Assert.NotNull(httpContext.User);
                Assert.NotNull(httpContext.User.Identity);
                Assert.True(httpContext.User.Identity.IsAuthenticated);
                httpContext.Response.OnCompleted(() =>
                {
                    userName = httpContext.User.Identity.Name;
                    completed.SetResult(0);
                    return Task.FromResult(0);
                });
                return Task.FromResult(0);
            }))
            {
                var response = await SendRequestAsync(address, useDefaultCredentials: true);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                await completed.Task.TimeoutAfter(TimeSpan.FromSeconds(5));
                Assert.False(string.IsNullOrEmpty(userName));
            }
        }

        [ConditionalTheory]
        [InlineData(AuthenticationSchemes.Negotiate)]
        [InlineData(AuthenticationSchemes.NTLM)]
        // [InlineData(AuthenticationSchemes.Digest)]
        [InlineData(AuthenticationSchemes.Basic)]
        [InlineData(AuthenticationSchemes.Negotiate | AuthenticationSchemes.NTLM | /*AuthenticationSchemes.Digest |*/ AuthenticationSchemes.Basic)]
        public async Task AuthTypes_AuthenticateWithNoUser_NoResults(AuthenticationSchemes authType)
        {
            var authTypeList = authType.ToString().Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            using (var server = Utilities.CreateDynamicHost(authType, AllowAnoymous, out var address, async httpContext =>
            {
                Assert.NotNull(httpContext.User);
                Assert.NotNull(httpContext.User.Identity);
                Assert.False(httpContext.User.Identity.IsAuthenticated);
                var authResults = await httpContext.AuthenticateAsync(HttpSysDefaults.AuthenticationScheme);
                Assert.False(authResults.Succeeded);
                Assert.True(authResults.None);
            }))
            {
                var response = await SendRequestAsync(address);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Empty(response.Headers.WwwAuthenticate);
            }
        }

        [ConditionalTheory]
        [InlineData(AuthenticationSchemes.Negotiate)]
        [InlineData(AuthenticationSchemes.NTLM)]
        // [InlineData(AuthenticationSchemes.Digest)]
        // [InlineData(AuthenticationSchemes.Basic)] // Doesn't work with default creds
        [InlineData(AuthenticationSchemes.Negotiate | AuthenticationSchemes.NTLM | /*AuthenticationSchemes.Digest |*/ AuthenticationSchemes.Basic)]
        public async Task AuthTypes_AuthenticateWithUser_OneResult(AuthenticationSchemes authType)
        {
            using (var server = Utilities.CreateDynamicHost(authType, DenyAnoymous, out var address, async httpContext =>
            {
                Assert.NotNull(httpContext.User);
                Assert.NotNull(httpContext.User.Identity);
                Assert.True(httpContext.User.Identity.IsAuthenticated);
                var authResults = await httpContext.AuthenticateAsync(HttpSysDefaults.AuthenticationScheme);
                Assert.True(authResults.Succeeded);
            }))
            {
                var response = await SendRequestAsync(address, useDefaultCredentials: true);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        [ConditionalTheory]
        [InlineData(AuthenticationSchemes.Negotiate)]
        [InlineData(AuthenticationSchemes.NTLM)]
        // [InlineData(AuthenticationSchemes.Digest)]
        [InlineData(AuthenticationSchemes.Basic)]
        [InlineData(AuthenticationSchemes.Negotiate | AuthenticationSchemes.NTLM | /*AuthenticationSchemes.Digest |*/ AuthenticationSchemes.Basic)]
        public async Task AuthTypes_ChallengeWithoutAuthTypes_AllChallengesSent(AuthenticationSchemes authType)
        {
            var authTypeList = authType.ToString().Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            using (var server = Utilities.CreateDynamicHost(authType, AllowAnoymous, out var address, httpContext =>
            {
                Assert.NotNull(httpContext.User);
                Assert.NotNull(httpContext.User.Identity);
                Assert.False(httpContext.User.Identity.IsAuthenticated);
                return httpContext.ChallengeAsync(HttpSysDefaults.AuthenticationScheme);
            }))
            {
                var response = await SendRequestAsync(address);
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                Assert.Equal(authTypeList.Count(), response.Headers.WwwAuthenticate.Count);
            }
        }

        [ConditionalTheory]
        [InlineData(AuthenticationSchemes.Negotiate)]
        [InlineData(AuthenticationSchemes.NTLM)]
        // [InlineData(AuthenticationSchemes.Digest)]
        [InlineData(AuthenticationSchemes.Basic)]
        [InlineData(AuthenticationSchemes.Negotiate | AuthenticationSchemes.NTLM | /*AuthenticationSchemes.Digest |*/ AuthenticationSchemes.Basic)]
        public async Task AuthTypes_ChallengeWithAllAuthTypes_AllChallengesSent(AuthenticationSchemes authType)
        {
            var authTypeList = authType.ToString().Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            using (var server = Utilities.CreateDynamicHost(authType, AllowAnoymous, out var address, async httpContext =>
            {
                Assert.NotNull(httpContext.User);
                Assert.NotNull(httpContext.User.Identity);
                Assert.False(httpContext.User.Identity.IsAuthenticated);
                await httpContext.ChallengeAsync(HttpSysDefaults.AuthenticationScheme);
            }))
            {
                var response = await SendRequestAsync(address);
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                Assert.Equal(authTypeList.Count(), response.Headers.WwwAuthenticate.Count);
            }
        }

        [ConditionalFact]
        public async Task AuthTypes_OneChallengeSent()
        {
            var authTypes = AuthenticationSchemes.Negotiate | AuthenticationSchemes.NTLM | /*AuthenticationSchemes.Digest |*/ AuthenticationSchemes.Basic;
            using (var server = Utilities.CreateDynamicHost(authTypes, AllowAnoymous, out var address, httpContext =>
            {
                Assert.NotNull(httpContext.User);
                Assert.NotNull(httpContext.User.Identity);
                Assert.False(httpContext.User.Identity.IsAuthenticated);
                return httpContext.ChallengeAsync(HttpSysDefaults.AuthenticationScheme);
            }))
            {
                var response = await SendRequestAsync(address);
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                Assert.Equal(3, response.Headers.WwwAuthenticate.Count);
            }
        }

        [ConditionalTheory]
        [InlineData(AuthenticationSchemes.Negotiate)]
        [InlineData(AuthenticationSchemes.NTLM)]
        // [InlineData(AuthenticationSchemes.Digest)]
        [InlineData(AuthenticationSchemes.Basic)]
        [InlineData(AuthenticationSchemes.Negotiate | AuthenticationSchemes.NTLM | /*AuthenticationSchemes.Digest |*/ AuthenticationSchemes.Basic)]
        [InlineData(AuthenticationSchemes.Negotiate | AuthenticationSchemes.NTLM)]
        [InlineData(AuthenticationSchemes.Negotiate | AuthenticationSchemes.Basic)]
        [InlineData(AuthenticationSchemes.NTLM | AuthenticationSchemes.Basic)]
        public async Task AuthTypes_ChallengeWillAskForAllEnabledSchemes(AuthenticationSchemes authType)
        {
            var authTypeList = authType.ToString().Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            using (var server = Utilities.CreateDynamicHost(authType, AllowAnoymous, out var address, httpContext =>
            {
                Assert.NotNull(httpContext.User);
                Assert.NotNull(httpContext.User.Identity);
                Assert.False(httpContext.User.Identity.IsAuthenticated);
                return httpContext.ChallengeAsync(HttpSysDefaults.AuthenticationScheme);
            }))
            {
                var response = await SendRequestAsync(address);
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                Assert.Equal(authTypeList.Count(), response.Headers.WwwAuthenticate.Count);
            }
        }

        [ConditionalFact]
        public async Task AuthTypes_Forbid_Forbidden()
        {
            var authTypes = AuthenticationSchemes.Negotiate | AuthenticationSchemes.NTLM | /*AuthenticationSchemes.Digest |*/ AuthenticationSchemes.Basic;
            using (var server = Utilities.CreateDynamicHost(authTypes, AllowAnoymous, out var address, httpContext =>
            {
                Assert.NotNull(httpContext.User);
                Assert.NotNull(httpContext.User.Identity);
                Assert.False(httpContext.User.Identity.IsAuthenticated);
                return httpContext.ForbidAsync(HttpSysDefaults.AuthenticationScheme);
            }))
            {
                var response = await SendRequestAsync(address);
                Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
                Assert.Empty(response.Headers.WwwAuthenticate);
            }
        }

        [ConditionalTheory]
        [InlineData(AuthenticationSchemes.Negotiate)]
        [InlineData(AuthenticationSchemes.NTLM)]
        // [InlineData(AuthenticationSchemes.Digest)] // Not implemented
        // [InlineData(AuthenticationSchemes.Basic)] // Can't log in with UseDefaultCredentials
        public async Task AuthTypes_UnathorizedAuthenticatedAuthType_Unauthorized(AuthenticationSchemes authType)
        {
            using (var server = Utilities.CreateDynamicHost(authType, DenyAnoymous, out var address, httpContext =>
            {
                Assert.NotNull(httpContext.User);
                Assert.NotNull(httpContext.User.Identity);
                Assert.True(httpContext.User.Identity.IsAuthenticated);
                return httpContext.ChallengeAsync(HttpSysDefaults.AuthenticationScheme, null);
            }))
            {
                var response = await SendRequestAsync(address, useDefaultCredentials: true);
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                Assert.Single(response.Headers.WwwAuthenticate);
                Assert.Equal(authType.ToString(), response.Headers.WwwAuthenticate.First().Scheme);
            }
        }

        [ConditionalTheory]
        [InlineData(AuthenticationSchemes.Negotiate)]
        [InlineData(AuthenticationSchemes.NTLM)]
        // [InlineData(AuthenticationSchemes.Digest)] // TODO: Not implemented
        // [InlineData(AuthenticationSchemes.Basic)] // Doesn't work with default creds
        [InlineData(AuthenticationSchemes.Negotiate | AuthenticationSchemes.NTLM | /* AuthenticationSchemes.Digest |*/ AuthenticationSchemes.Basic)]
        public async Task AuthTypes_DisableAutomaticAuthentication(AuthenticationSchemes authType)
        {
            using (var server = Utilities.CreateDynamicHost(out var address, options =>
            {
                options.Authentication.AutomaticAuthentication = false;
                options.Authentication.Schemes = authType;
                options.Authentication.AllowAnonymous = DenyAnoymous;
            },
            async httpContext =>
            {
                Assert.NotNull(httpContext.User);
                Assert.NotNull(httpContext.User.Identity);
                Assert.False(httpContext.User.Identity.IsAuthenticated);

                var authenticateResult = await httpContext.AuthenticateAsync(HttpSysDefaults.AuthenticationScheme);

                Assert.NotNull(authenticateResult.Principal);
                Assert.NotNull(authenticateResult.Principal.Identity);
                Assert.True(authenticateResult.Principal.Identity.IsAuthenticated);
            }))
            {
                var response = await SendRequestAsync(address, useDefaultCredentials: true);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        private async Task<HttpResponseMessage> SendRequestAsync(string uri, bool useDefaultCredentials = false)
        {
            HttpClientHandler handler = new HttpClientHandler();
            handler.UseDefaultCredentials = useDefaultCredentials;
            using (HttpClient client = new HttpClient(handler))
            {
                return await client.GetAsync(uri);
            }
        }
    }
}
