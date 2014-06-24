// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Net.Server
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
            using (var server = Utilities.CreateAuthServer(authType | AuthenticationTypes.AllowAnonymous))
            {
                Task<HttpResponseMessage> responseTask = SendRequestAsync(Address);

                var context = await server.GetContextAsync();
                Assert.NotNull(context.User);
                Assert.False(context.User.Identity.IsAuthenticated);
                if (authType == AuthenticationTypes.AllowAnonymous)
                {
                    Assert.Equal(AuthenticationTypes.None, context.AuthenticationChallenges);
                }
                else
                {
                    Assert.Equal(authType, context.AuthenticationChallenges);
                }
                context.Dispose();

                var response = await responseTask;
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal(0, response.Headers.WwwAuthenticate.Count);
            }
        }

        [Theory]
        [InlineData(AuthenticationTypes.Kerberos)]
        [InlineData(AuthenticationTypes.Negotiate)]
        [InlineData(AuthenticationTypes.NTLM)]
        // [InlineData(AuthenticationType.Digest)] // TODO: Not implemented
        [InlineData(AuthenticationTypes.Basic)]
        public async Task AuthType_RequireAuth_ChallengesAdded(AuthenticationTypes authType)
        {
            using (var server = Utilities.CreateAuthServer(authType))
            {
                Task<HttpResponseMessage> responseTask = SendRequestAsync(Address);

                var contextTask = server.GetContextAsync(); // Fails when the server shuts down, the challenge happens internally.

                var response = await responseTask;
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
            using (var server = Utilities.CreateAuthServer(authType | AuthenticationTypes.AllowAnonymous))
            {
                Task<HttpResponseMessage> responseTask = SendRequestAsync(Address);

                var context = await server.GetContextAsync();
                Assert.NotNull(context.User);
                Assert.False(context.User.Identity.IsAuthenticated);
                Assert.Equal(authType, context.AuthenticationChallenges);
                context.Response.StatusCode = 401;
                context.Dispose();

                var response = await responseTask;
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                Assert.Equal(authType.ToString(), response.Headers.WwwAuthenticate.ToString(), StringComparer.OrdinalIgnoreCase);
            }
        }

        [Fact]
        public async Task MultipleAuthTypes_AllowAnonymousButSpecify401_ChallengesAdded()
        {
            AuthenticationTypes authType =
                AuthenticationTypes.Kerberos
                | AuthenticationTypes.Negotiate
                | AuthenticationTypes.NTLM
                /* | AuthenticationTypes.Digest TODO: Not implemented */
                | AuthenticationTypes.Basic;
            using (var server = Utilities.CreateAuthServer(authType | AuthenticationTypes.AllowAnonymous))
            {
                Task<HttpResponseMessage> responseTask = SendRequestAsync(Address);

                var context = await server.GetContextAsync();
                Assert.NotNull(context.User);
                Assert.False(context.User.Identity.IsAuthenticated);
                Assert.Equal(authType, context.AuthenticationChallenges);
                context.Response.StatusCode = 401;
                context.Dispose();

                var response = await responseTask;
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
        [InlineData(AuthenticationTypes.Kerberos | AuthenticationTypes.Negotiate | AuthenticationTypes.NTLM | /*AuthenticationType.Digest |*/ AuthenticationTypes.Basic)]
        public async Task AuthTypes_AllowAnonymousButSpecify401_Success(AuthenticationTypes authType)
        {
            using (var server = Utilities.CreateAuthServer(authType | AuthenticationTypes.AllowAnonymous))
            {
                Task<HttpResponseMessage> responseTask = SendRequestAsync(Address, useDefaultCredentials: true);

                var context = await server.GetContextAsync();
                Assert.NotNull(context.User);
                Assert.False(context.User.Identity.IsAuthenticated);
                Assert.Equal(authType, context.AuthenticationChallenges);
                context.Response.StatusCode = 401;
                context.Dispose();

                context = await server.GetContextAsync();
                Assert.NotNull(context.User);
                Assert.True(context.User.Identity.IsAuthenticated);
                Assert.Equal(authType, context.AuthenticationChallenges);
                context.Dispose();

                var response = await responseTask;
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        [Theory]
        [InlineData(AuthenticationTypes.Kerberos)]
        [InlineData(AuthenticationTypes.Negotiate)]
        [InlineData(AuthenticationTypes.NTLM)]
        // [InlineData(AuthenticationTypes.Digest)] // TODO: Not implemented
        // [InlineData(AuthenticationTypes.Basic)] // Doesn't work with default creds
        [InlineData(AuthenticationTypes.Kerberos | AuthenticationTypes.Negotiate | AuthenticationTypes.NTLM | /*AuthenticationType.Digest |*/ AuthenticationTypes.Basic)]
        public async Task AuthTypes_RequireAuth_Success(AuthenticationTypes authType)
        {
            using (var server = Utilities.CreateAuthServer(authType))
            {
                Task<HttpResponseMessage> responseTask = SendRequestAsync(Address, useDefaultCredentials: true);

                var context = await server.GetContextAsync();
                Assert.NotNull(context.User);
                Assert.True(context.User.Identity.IsAuthenticated);
                Assert.Equal(authType, context.AuthenticationChallenges);
                context.Dispose();

                var response = await responseTask;
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