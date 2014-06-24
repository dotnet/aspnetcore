// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNet.FeatureModel;
using Microsoft.AspNet.PipelineCore;
using Microsoft.Net.Server;
using Xunit;

namespace Microsoft.AspNet.Server.WebListener
{
    public class AuthenticationTests
    {
        private const string Address = "http://localhost:8080/";

        [Theory]
        [InlineData(AuthenticationTypes.AllowAnonymous)]
        [InlineData(AuthenticationTypes.Kerberos)]
        [InlineData(AuthenticationTypes.Negotiate)]
        [InlineData(AuthenticationTypes.NTLM)]
        // [InlineData(AuthenticationTypes.Digest)]
        [InlineData(AuthenticationTypes.Basic)]
        [InlineData(AuthenticationTypes.Kerberos | AuthenticationTypes.Negotiate | AuthenticationTypes.NTLM | /*AuthenticationTypes.Digest |*/ AuthenticationTypes.Basic)]
        public async Task AuthTypes_AllowAnonymous_NoChallenge(AuthenticationTypes authType)
        {
            using (Utilities.CreateAuthServer(authType | AuthenticationTypes.AllowAnonymous, env =>
            {
                var context = new DefaultHttpContext((IFeatureCollection)env);
                Assert.NotNull(context.User);
                Assert.False(context.User.Identity.IsAuthenticated);
                return Task.FromResult(0);
            }))
            {
                var response = await SendRequestAsync(Address);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal(0, response.Headers.WwwAuthenticate.Count);
            }
        }

        [Theory]
        [InlineData(AuthenticationTypes.Kerberos)]
        [InlineData(AuthenticationTypes.Negotiate)]
        [InlineData(AuthenticationTypes.NTLM)]
        // [InlineData(AuthenticationTypes.Digest)] // TODO: Not implemented
        [InlineData(AuthenticationTypes.Basic)]
        public async Task AuthType_RequireAuth_ChallengesAdded(AuthenticationTypes authType)
        {
            using (Utilities.CreateAuthServer(authType, env =>
            {
                throw new NotImplementedException();
            }))
            {
                var response = await SendRequestAsync(Address);
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                Assert.Equal(authType.ToString(), response.Headers.WwwAuthenticate.ToString(), StringComparer.OrdinalIgnoreCase);
            }
        }

        [Theory]
        [InlineData(AuthenticationTypes.Kerberos)]
        [InlineData(AuthenticationTypes.Negotiate)]
        [InlineData(AuthenticationTypes.NTLM)]
        // [InlineData(AuthenticationTypes.Digest)] // TODO: Not implemented
        [InlineData(AuthenticationTypes.Basic)]
        public async Task AuthType_AllowAnonymousButSpecify401_ChallengesAdded(AuthenticationTypes authType)
        {
            using (Utilities.CreateAuthServer(authType | AuthenticationTypes.AllowAnonymous, env =>
            {
                var context = new DefaultHttpContext((IFeatureCollection)env);
                Assert.NotNull(context.User);
                Assert.False(context.User.Identity.IsAuthenticated);
                context.Response.StatusCode = 401;
                return Task.FromResult(0);
            }))
            {
                var response = await SendRequestAsync(Address);
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                Assert.Equal(authType.ToString(), response.Headers.WwwAuthenticate.ToString(), StringComparer.OrdinalIgnoreCase);
            }
        }

        [Fact]
        public async Task MultipleAuthTypes_AllowAnonymousButSpecify401_ChallengesAdded()
        {
            using (Utilities.CreateAuthServer(
                AuthenticationTypes.Kerberos
                | AuthenticationTypes.Negotiate
                | AuthenticationTypes.NTLM
                /* | AuthenticationTypes.Digest TODO: Not implemented */
                | AuthenticationTypes.Basic
                | AuthenticationTypes.AllowAnonymous,
                env =>
            {
                var context = new DefaultHttpContext((IFeatureCollection)env);
                Assert.NotNull(context.User);
                Assert.False(context.User.Identity.IsAuthenticated);
                context.Response.StatusCode = 401;
                return Task.FromResult(0);
            }))
            {
                var response = await SendRequestAsync(Address);
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                Assert.Equal("Kerberos, Negotiate, NTLM, basic", response.Headers.WwwAuthenticate.ToString(), StringComparer.OrdinalIgnoreCase);
            }
        }

        [Theory]
        [InlineData(AuthenticationTypes.Kerberos)]
        [InlineData(AuthenticationTypes.Negotiate)]
        [InlineData(AuthenticationTypes.NTLM)]
        // [InlineData(AuthenticationTypes.Digest)] // TODO: Not implemented
        // [InlineData(AuthenticationTypes.Basic)] // Doesn't work with default creds
        [InlineData(AuthenticationTypes.Kerberos | AuthenticationTypes.Negotiate | AuthenticationTypes.NTLM | /* AuthenticationTypes.Digest |*/ AuthenticationTypes.Basic)]
        public async Task AuthTypes_AllowAnonymousButSpecify401_Success(AuthenticationTypes authType)
        {
            int requestId = 0;
            using (Utilities.CreateAuthServer(authType | AuthenticationTypes.AllowAnonymous, env =>
            {
                var context = new DefaultHttpContext((IFeatureCollection)env);
                Assert.NotNull(context.User);
                if (requestId == 0)
                {
                    Assert.False(context.User.Identity.IsAuthenticated);
                    context.Response.StatusCode = 401;
                }
                else if (requestId == 1)
                {
                    Assert.True(context.User.Identity.IsAuthenticated);
                }
                else
                {
                    throw new NotImplementedException();
                }
                requestId++;
                return Task.FromResult(0);
            }))
            {
                var response = await SendRequestAsync(Address, useDefaultCredentials: true);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        [Theory]
        [InlineData(AuthenticationTypes.Kerberos)]
        [InlineData(AuthenticationTypes.Negotiate)]
        [InlineData(AuthenticationTypes.NTLM)]
        // [InlineData(AuthenticationTypes.Digest)] // TODO: Not implemented
        // [InlineData(AuthenticationTypes.Basic)] // Doesn't work with default creds
        [InlineData(AuthenticationTypes.Kerberos | AuthenticationTypes.Negotiate | AuthenticationTypes.NTLM | /* AuthenticationTypes.Digest |*/ AuthenticationTypes.Basic)]
        public async Task AuthTypes_RequireAuth_Success(AuthenticationTypes authType)
        {
            using (Utilities.CreateAuthServer(authType, env =>
            {
                var context = new DefaultHttpContext((IFeatureCollection)env);
                Assert.NotNull(context.User);
                Assert.True(context.User.Identity.IsAuthenticated);
                return Task.FromResult(0);
            }))
            {
                var response = await SendRequestAsync(Address, useDefaultCredentials: true);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        [Theory]
        [InlineData(AuthenticationTypes.AllowAnonymous)]
        [InlineData(AuthenticationTypes.Kerberos)]
        [InlineData(AuthenticationTypes.Negotiate)]
        [InlineData(AuthenticationTypes.NTLM)]
        // [InlineData(AuthenticationTypes.Digest)]
        [InlineData(AuthenticationTypes.Basic)]
        // [InlineData(AuthenticationTypes.Kerberos | AuthenticationTypes.Negotiate | AuthenticationTypes.NTLM | /*AuthenticationTypes.Digest |*/ AuthenticationTypes.Basic)]
        public async Task AuthTypes_GetSingleDescriptions(AuthenticationTypes authType)
        {
            using (Utilities.CreateAuthServer(authType | AuthenticationTypes.AllowAnonymous, env =>
            {
                var context = new DefaultHttpContext((IFeatureCollection)env);
                var resultList = context.GetAuthenticationTypes();
                if (authType == AuthenticationTypes.AllowAnonymous)
                {
                    Assert.Equal(0, resultList.Count());
                }
                else
                {
                    Assert.Equal(1, resultList.Count());
                    var result = resultList.First();
                    Assert.Equal(authType.ToString(), result.AuthenticationType);
                    Assert.Equal("Windows:" + authType.ToString(), result.Caption);
                }

                return Task.FromResult(0);
            }))
            {
                var response = await SendRequestAsync(Address);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal(0, response.Headers.WwwAuthenticate.Count);
            }
        }

        [Fact]
        public async Task AuthTypes_GetMultipleDescriptions()
        {
            AuthenticationTypes authType =
                AuthenticationTypes.Kerberos
                | AuthenticationTypes.Negotiate
                | AuthenticationTypes.NTLM
                | /*AuthenticationTypes.Digest
                |*/ AuthenticationTypes.Basic;
            using (Utilities.CreateAuthServer(authType | AuthenticationTypes.AllowAnonymous, env =>
            {
                var context = new DefaultHttpContext((IFeatureCollection)env);
                var resultList = context.GetAuthenticationTypes();
                Assert.Equal(4, resultList.Count());
                return Task.FromResult(0);
            }))
            {
                var response = await SendRequestAsync(Address);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal(0, response.Headers.WwwAuthenticate.Count);
            }
        }

        [Theory]
        [InlineData(AuthenticationTypes.Kerberos)]
        [InlineData(AuthenticationTypes.Negotiate)]
        [InlineData(AuthenticationTypes.NTLM)]
        // [InlineData(AuthenticationTypes.Digest)]
        [InlineData(AuthenticationTypes.Basic)]
        [InlineData(AuthenticationTypes.Kerberos | AuthenticationTypes.Negotiate | AuthenticationTypes.NTLM | /*AuthenticationTypes.Digest |*/ AuthenticationTypes.Basic)]
        public async Task AuthTypes_AuthenticateWithNoUser_NoResults(AuthenticationTypes authType)
        {
            var authTypeList = authType.ToString().Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            using (Utilities.CreateAuthServer(authType | AuthenticationTypes.AllowAnonymous, env =>
            {
                var context = new DefaultHttpContext((IFeatureCollection)env);
                Assert.NotNull(context.User);
                Assert.False(context.User.Identity.IsAuthenticated);
                var authResults = context.Authenticate(authTypeList);
                Assert.False(authResults.Any());
                return Task.FromResult(0);
            }))
            {
                var response = await SendRequestAsync(Address);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal(0, response.Headers.WwwAuthenticate.Count);
            }
        }

        [Theory]
        [InlineData(AuthenticationTypes.Kerberos)]
        [InlineData(AuthenticationTypes.Negotiate)]
        [InlineData(AuthenticationTypes.NTLM)]
        // [InlineData(AuthenticationTypes.Digest)]
        // [InlineData(AuthenticationTypes.Basic)] // Doesn't work with default creds
        [InlineData(AuthenticationTypes.Kerberos | AuthenticationTypes.Negotiate | AuthenticationTypes.NTLM | /*AuthenticationTypes.Digest |*/ AuthenticationTypes.Basic)]
        public async Task AuthTypes_AuthenticateWithUser_OneResult(AuthenticationTypes authType)
        {
            var authTypeList = authType.ToString().Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            using (Utilities.CreateAuthServer(authType, env =>
            {
                var context = new DefaultHttpContext((IFeatureCollection)env);
                Assert.NotNull(context.User);
                Assert.True(context.User.Identity.IsAuthenticated);
                var authResults = context.Authenticate(authTypeList);
                Assert.Equal(1, authResults.Count());
                return Task.FromResult(0);
            }))
            {
                var response = await SendRequestAsync(Address, useDefaultCredentials: true);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        [Theory]
        [InlineData(AuthenticationTypes.Kerberos)]
        [InlineData(AuthenticationTypes.Negotiate)]
        [InlineData(AuthenticationTypes.NTLM)]
        // [InlineData(AuthenticationTypes.Digest)]
        [InlineData(AuthenticationTypes.Basic)]
        [InlineData(AuthenticationTypes.Kerberos | AuthenticationTypes.Negotiate | AuthenticationTypes.NTLM | /*AuthenticationTypes.Digest |*/ AuthenticationTypes.Basic)]
        public async Task AuthTypes_ChallengeWithoutAuthTypes_AllChallengesSent(AuthenticationTypes authType)
        {
            var authTypeList = authType.ToString().Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            using (Utilities.CreateAuthServer(authType | AuthenticationTypes.AllowAnonymous, env =>
            {
                var context = new DefaultHttpContext((IFeatureCollection)env);
                Assert.NotNull(context.User);
                Assert.False(context.User.Identity.IsAuthenticated);
                context.Response.Challenge();
                return Task.FromResult(0);
            }))
            {
                var response = await SendRequestAsync(Address);
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                Assert.Equal(authTypeList.Count(), response.Headers.WwwAuthenticate.Count);
            }
        }

        [Theory]
        [InlineData(AuthenticationTypes.Kerberos)]
        [InlineData(AuthenticationTypes.Negotiate)]
        [InlineData(AuthenticationTypes.NTLM)]
        // [InlineData(AuthenticationTypes.Digest)]
        [InlineData(AuthenticationTypes.Basic)]
        [InlineData(AuthenticationTypes.Kerberos | AuthenticationTypes.Negotiate | AuthenticationTypes.NTLM | /*AuthenticationTypes.Digest |*/ AuthenticationTypes.Basic)]
        public async Task AuthTypes_ChallengeWithAllAuthTypes_AllChallengesSent(AuthenticationTypes authType)
        {
            var authTypeList = authType.ToString().Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            using (Utilities.CreateAuthServer(authType | AuthenticationTypes.AllowAnonymous, env =>
            {
                var context = new DefaultHttpContext((IFeatureCollection)env);
                Assert.NotNull(context.User);
                Assert.False(context.User.Identity.IsAuthenticated);
                context.Response.Challenge(authTypeList);
                return Task.FromResult(0);
            }))
            {
                var response = await SendRequestAsync(Address);
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                Assert.Equal(authTypeList.Count(), response.Headers.WwwAuthenticate.Count);
            }
        }

        [Theory]
        [InlineData(AuthenticationTypes.Kerberos)]
        [InlineData(AuthenticationTypes.Negotiate)]
        [InlineData(AuthenticationTypes.NTLM)]
        // [InlineData(AuthenticationTypes.Digest)]
        [InlineData(AuthenticationTypes.Basic)]
        public async Task AuthTypes_ChallengeOneAuthType_OneChallengeSent(AuthenticationTypes authType)
        {
            var authTypes = AuthenticationTypes.Kerberos | AuthenticationTypes.Negotiate | AuthenticationTypes.NTLM | /*AuthenticationTypes.Digest |*/ AuthenticationTypes.Basic;
            using (Utilities.CreateAuthServer(authTypes | AuthenticationTypes.AllowAnonymous, env =>
            {
                var context = new DefaultHttpContext((IFeatureCollection)env);
                Assert.NotNull(context.User);
                Assert.False(context.User.Identity.IsAuthenticated);
                context.Response.Challenge(authType.ToString());
                return Task.FromResult(0);
            }))
            {
                var response = await SendRequestAsync(Address);
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                Assert.Equal(1, response.Headers.WwwAuthenticate.Count);
                Assert.Equal(authType.ToString(), response.Headers.WwwAuthenticate.First().Scheme);
            }
        }

        [Theory]
        [InlineData(AuthenticationTypes.Kerberos)]
        [InlineData(AuthenticationTypes.Negotiate)]
        [InlineData(AuthenticationTypes.NTLM)]
        // [InlineData(AuthenticationTypes.Digest)]
        [InlineData(AuthenticationTypes.Basic)]
        public async Task AuthTypes_ChallengeDisabledAuthType_Error(AuthenticationTypes authType)
        {
            var authTypes = AuthenticationTypes.Kerberos | AuthenticationTypes.Negotiate | AuthenticationTypes.NTLM | /*AuthenticationTypes.Digest |*/ AuthenticationTypes.Basic;
            authTypes = authTypes & ~authType;
            var authTypeList = authType.ToString().Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            using (Utilities.CreateAuthServer(authTypes | AuthenticationTypes.AllowAnonymous, env =>
            {
                var context = new DefaultHttpContext((IFeatureCollection)env);
                Assert.NotNull(context.User);
                Assert.False(context.User.Identity.IsAuthenticated);
                Assert.Throws<InvalidOperationException>(() => context.Response.Challenge(authType.ToString()));
                return Task.FromResult(0);
            }))
            {
                var response = await SendRequestAsync(Address);
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                Assert.Equal(0, response.Headers.WwwAuthenticate.Count);
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
