// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace Microsoft.Net.Http.Server
{
    public class AuthenticationTests
    {
        [Theory]
        [InlineData(AuthenticationSchemes.AllowAnonymous)]
        [InlineData(AuthenticationSchemes.Negotiate)]
        [InlineData(AuthenticationSchemes.NTLM)]
        // [InlineData(AuthenticationSchemes.Digest)]
        [InlineData(AuthenticationSchemes.Basic)]
        [InlineData(AuthenticationSchemes.Negotiate | AuthenticationSchemes.NTLM | /*AuthenticationSchemes.Digest |*/ AuthenticationSchemes.Basic)]
        public async Task AuthTypes_AllowAnonymous_NoChallenge(AuthenticationSchemes authType)
        {
            string address;
            using (var server = Utilities.CreateHttpAuthServer(authType | AuthenticationSchemes.AllowAnonymous, out address))
            {
                Task<HttpResponseMessage> responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync();
                Assert.NotNull(context.User);
                Assert.False(context.User.Identity.IsAuthenticated);
                if (authType == AuthenticationSchemes.AllowAnonymous)
                {
                    Assert.Equal(AuthenticationSchemes.None, context.Response.AuthenticationChallenges);
                }
                else
                {
                    Assert.Equal(authType, context.Response.AuthenticationChallenges);
                }
                context.Dispose();

                var response = await responseTask;
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Equal(0, response.Headers.WwwAuthenticate.Count);
            }
        }

        [ConditionalTheory]
        [InlineData(AuthenticationSchemes.Negotiate)]
        [InlineData(AuthenticationSchemes.NTLM)]
        // [InlineData(AuthenticationType.Digest)] // TODO: Not implemented
        [InlineData(AuthenticationSchemes.Basic)]
        [FrameworkSkipCondition(RuntimeFrameworks.CoreCLR, SkipReason = "HttpClientHandler issue (https://github.com/dotnet/corefx/issues/5045).")]
        public async Task AuthType_RequireAuth_ChallengesAdded(AuthenticationSchemes authType)
        {
            string address;
            using (var server = Utilities.CreateHttpAuthServer(authType, out address))
            {
                Task<HttpResponseMessage> responseTask = SendRequestAsync(address);

                var contextTask = server.AcceptAsync(); // Fails when the server shuts down, the challenge happens internally.
                var response = await responseTask;
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                Assert.Equal(authType.ToString(), response.Headers.WwwAuthenticate.ToString(), StringComparer.OrdinalIgnoreCase);
            }
        }

        [ConditionalTheory]
        [InlineData(AuthenticationSchemes.Negotiate)]
        [InlineData(AuthenticationSchemes.NTLM)]
        // [InlineData(AuthenticationSchemes.Digest)] // TODO: Not implemented
        [InlineData(AuthenticationSchemes.Basic)]
        [FrameworkSkipCondition(RuntimeFrameworks.CoreCLR, SkipReason = "HttpClientHandler issue (https://github.com/dotnet/corefx/issues/5045).")]
        public async Task AuthType_AllowAnonymousButSpecify401_ChallengesAdded(AuthenticationSchemes authType)
        {
            string address;
            using (var server = Utilities.CreateHttpAuthServer(authType | AuthenticationSchemes.AllowAnonymous, out address))
            {
                Task<HttpResponseMessage> responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync();
                Assert.NotNull(context.User);
                Assert.False(context.User.Identity.IsAuthenticated);
                Assert.Equal(authType, context.Response.AuthenticationChallenges);
                context.Response.StatusCode = 401;
                context.Dispose();

                var response = await responseTask;
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                Assert.Equal(authType.ToString(), response.Headers.WwwAuthenticate.ToString(), StringComparer.OrdinalIgnoreCase);
            }
        }

        [ConditionalFact]
        [FrameworkSkipCondition(RuntimeFrameworks.CoreCLR, SkipReason = "HttpClientHandler issue (https://github.com/dotnet/corefx/issues/5045).")]
        public async Task MultipleAuthTypes_AllowAnonymousButSpecify401_ChallengesAdded()
        {
            string address;
            AuthenticationSchemes authType =
                AuthenticationSchemes.Negotiate
                | AuthenticationSchemes.NTLM
                /* | AuthenticationSchemes.Digest TODO: Not implemented */
                | AuthenticationSchemes.Basic;
            using (var server = Utilities.CreateHttpAuthServer(authType | AuthenticationSchemes.AllowAnonymous, out address))
            {
                Task<HttpResponseMessage> responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync();
                Assert.NotNull(context.User);
                Assert.False(context.User.Identity.IsAuthenticated);
                Assert.Equal(authType, context.Response.AuthenticationChallenges);
                context.Response.StatusCode = 401;
                context.Dispose();

                var response = await responseTask;
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                Assert.Equal("Negotiate, NTLM, basic", response.Headers.WwwAuthenticate.ToString(), StringComparer.OrdinalIgnoreCase);
            }
        }

        [Theory]
        [InlineData(AuthenticationSchemes.Negotiate)]
        [InlineData(AuthenticationSchemes.NTLM)]
        // [InlineData(AuthenticationSchemes.Digest)] // TODO: Not implemented
        // [InlineData(AuthenticationSchemes.Basic)] // Doesn't work with default creds
        [InlineData(AuthenticationSchemes.Negotiate | AuthenticationSchemes.NTLM | /*AuthenticationType.Digest |*/ AuthenticationSchemes.Basic)]
        public async Task AuthTypes_AllowAnonymousButSpecify401_Success(AuthenticationSchemes authType)
        {
            string address;
            using (var server = Utilities.CreateHttpAuthServer(authType | AuthenticationSchemes.AllowAnonymous, out address))
            {
                Task<HttpResponseMessage> responseTask = SendRequestAsync(address, useDefaultCredentials: true);

                var context = await server.AcceptAsync();
                Assert.NotNull(context.User);
                Assert.False(context.User.Identity.IsAuthenticated);
                Assert.Equal(authType, context.Response.AuthenticationChallenges);
                context.Response.StatusCode = 401;
                context.Dispose();

                context = await server.AcceptAsync();
                Assert.NotNull(context.User);
                Assert.True(context.User.Identity.IsAuthenticated);
                Assert.Equal(authType, context.Response.AuthenticationChallenges);
                context.Dispose();

                var response = await responseTask;
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        [Theory]
        [InlineData(AuthenticationSchemes.Negotiate)]
        [InlineData(AuthenticationSchemes.NTLM)]
        // [InlineData(AuthenticationSchemes.Digest)] // TODO: Not implemented
        // [InlineData(AuthenticationSchemes.Basic)] // Doesn't work with default creds
        [InlineData(AuthenticationSchemes.Negotiate | AuthenticationSchemes.NTLM | /*AuthenticationType.Digest |*/ AuthenticationSchemes.Basic)]
        public async Task AuthTypes_RequireAuth_Success(AuthenticationSchemes authType)
        {
            string address;
            using (var server = Utilities.CreateHttpAuthServer(authType, out address))
            {
                Task<HttpResponseMessage> responseTask = SendRequestAsync(address, useDefaultCredentials: true);

                var context = await server.AcceptAsync();
                Assert.NotNull(context.User);
                Assert.True(context.User.Identity.IsAuthenticated);
                Assert.Equal(authType, context.Response.AuthenticationChallenges);
                context.Dispose();

                var response = await responseTask;
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        [ConditionalTheory]
        [SkipOffDomain]
        public async Task AuthTypes_RequireKerberosAuth_Success()
        {
            string address;
            using (var server = Utilities.CreateHttpAuthServer(AuthenticationSchemes.Kerberos, out address))
            {
                Task<HttpResponseMessage> responseTask = SendRequestAsync(address, useDefaultCredentials: true);

                var context = await server.AcceptAsync();
                Assert.NotNull(context.User);
                Assert.True(context.User.Identity.IsAuthenticated);
                Assert.Equal(AuthenticationSchemes.Kerberos, context.Response.AuthenticationChallenges);
                context.Dispose();

                var response = await responseTask;
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        [ConditionalTheory]
        [SkipOffDomain]
        public async Task MultipleAuthTypes_KerberosAllowAnonymousButSpecify401_ChallengesAdded()
        {
            string address;
            using (var server = Utilities.CreateHttpAuthServer(AuthenticationSchemes.Kerberos | AuthenticationSchemes.AllowAnonymous, out address))
            {
                Task<HttpResponseMessage> responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync();
                Assert.NotNull(context.User);
                Assert.False(context.User.Identity.IsAuthenticated);
                Assert.Equal(AuthenticationSchemes.Kerberos, context.Response.AuthenticationChallenges);
                context.Response.StatusCode = 401;
                context.Dispose();

                var response = await responseTask;
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                Assert.Equal("Kerberos", response.Headers.WwwAuthenticate.ToString(), StringComparer.OrdinalIgnoreCase);
            }
        }

        private async Task<HttpResponseMessage> SendRequestAsync(string uri, bool useDefaultCredentials = false)
        {
            HttpClientHandler handler = new HttpClientHandler();
            handler.UseDefaultCredentials = useDefaultCredentials;
            using (HttpClient client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(5) })
            {
                return await client.GetAsync(uri);
            }
        }
    }
}