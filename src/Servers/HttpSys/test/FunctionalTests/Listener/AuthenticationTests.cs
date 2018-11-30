// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace Microsoft.AspNetCore.Server.HttpSys.Listener
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
            string address;
            using (var server = Utilities.CreateHttpAuthServer(authType, AllowAnoymous, out address))
            {
                Task<HttpResponseMessage> responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout);
                Assert.NotNull(context.User);
                Assert.False(context.User.Identity.IsAuthenticated);
                Assert.Equal(authType, context.Response.AuthenticationChallenges);
                context.Dispose();

                var response = await responseTask;
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Empty(response.Headers.WwwAuthenticate);
            }
        }
#if !NETCOREAPP2_0
        // https://github.com/aspnet/ServerTests/issues/82
        [ConditionalTheory]
        [InlineData(AuthenticationSchemes.Negotiate)]
        [InlineData(AuthenticationSchemes.NTLM)]
        // [InlineData(AuthenticationType.Digest)] // TODO: Not implemented
        [InlineData(AuthenticationSchemes.Basic)]
        public async Task AuthType_RequireAuth_ChallengesAdded(AuthenticationSchemes authType)
        {
            string address;
            using (var server = Utilities.CreateHttpAuthServer(authType, DenyAnoymous, out address))
            {
                Task<HttpResponseMessage> responseTask = SendRequestAsync(address);

                var contextTask = server.AcceptAsync(Utilities.DefaultTimeout); // Fails when the server shuts down, the challenge happens internally.
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
        public async Task AuthType_AllowAnonymousButSpecify401_ChallengesAdded(AuthenticationSchemes authType)
        {
            string address;
            using (var server = Utilities.CreateHttpAuthServer(authType, AllowAnoymous, out address))
            {
                Task<HttpResponseMessage> responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout);
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
        public async Task MultipleAuthTypes_AllowAnonymousButSpecify401_ChallengesAdded()
        {
            string address;
            AuthenticationSchemes authType =
                AuthenticationSchemes.Negotiate
                | AuthenticationSchemes.NTLM
                /* | AuthenticationSchemes.Digest TODO: Not implemented */
                | AuthenticationSchemes.Basic;
            using (var server = Utilities.CreateHttpAuthServer(authType, AllowAnoymous, out address))
            {
                Task<HttpResponseMessage> responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout);
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
#endif
        [ConditionalTheory]
        [InlineData(AuthenticationSchemes.Negotiate)]
        [InlineData(AuthenticationSchemes.NTLM)]
        // [InlineData(AuthenticationSchemes.Digest)] // TODO: Not implemented
        // [InlineData(AuthenticationSchemes.Basic)] // Doesn't work with default creds
        [InlineData(AuthenticationSchemes.Negotiate | AuthenticationSchemes.NTLM | /*AuthenticationType.Digest |*/ AuthenticationSchemes.Basic)]
        public async Task AuthTypes_AllowAnonymousButSpecify401_Success(AuthenticationSchemes authType)
        {
            string address;
            using (var server = Utilities.CreateHttpAuthServer(authType, AllowAnoymous, out address))
            {
                Task<HttpResponseMessage> responseTask = SendRequestAsync(address, useDefaultCredentials: true);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout);
                Assert.NotNull(context.User);
                Assert.False(context.User.Identity.IsAuthenticated);
                Assert.Equal(authType, context.Response.AuthenticationChallenges);
                context.Response.StatusCode = 401;
                context.Dispose();

                context = await server.AcceptAsync(Utilities.DefaultTimeout);
                Assert.NotNull(context.User);
                Assert.True(context.User.Identity.IsAuthenticated);
                Assert.Equal(authType, context.Response.AuthenticationChallenges);
                context.Dispose();

                var response = await responseTask;
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        [ConditionalTheory]
        [InlineData(AuthenticationSchemes.Negotiate)]
        [InlineData(AuthenticationSchemes.NTLM)]
        // [InlineData(AuthenticationSchemes.Digest)] // TODO: Not implemented
        // [InlineData(AuthenticationSchemes.Basic)] // Doesn't work with default creds
        [InlineData(AuthenticationSchemes.Negotiate | AuthenticationSchemes.NTLM | /*AuthenticationType.Digest |*/ AuthenticationSchemes.Basic)]
        public async Task AuthTypes_RequireAuth_Success(AuthenticationSchemes authType)
        {
            string address;
            using (var server = Utilities.CreateHttpAuthServer(authType, DenyAnoymous, out address))
            {
                Task<HttpResponseMessage> responseTask = SendRequestAsync(address, useDefaultCredentials: true);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout);
                Assert.NotNull(context.User);
                Assert.True(context.User.Identity.IsAuthenticated);
                Assert.Equal(authType, context.Response.AuthenticationChallenges);
                context.Dispose();

                var response = await responseTask;
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        [ConditionalFact(Skip = "Requires a domain joined machine - https://github.com/aspnet/HttpSysServer/issues/357")]
        public async Task AuthTypes_RequireKerberosAuth_Success()
        {
            string address;
            using (var server = Utilities.CreateHttpAuthServer(AuthenticationSchemes.Kerberos, DenyAnoymous, out address))
            {
                Task<HttpResponseMessage> responseTask = SendRequestAsync(address, useDefaultCredentials: true);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout);
                Assert.NotNull(context.User);
                Assert.True(context.User.Identity.IsAuthenticated);
                Assert.Equal(AuthenticationSchemes.Kerberos, context.Response.AuthenticationChallenges);
                context.Dispose();

                var response = await responseTask;
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        [ConditionalFact(Skip = "Requires a domain joined machine - https://github.com/aspnet/HttpSysServer/issues/357")]
        public async Task MultipleAuthTypes_KerberosAllowAnonymousButSpecify401_ChallengesAdded()
        {
            string address;
            using (var server = Utilities.CreateHttpAuthServer(AuthenticationSchemes.Kerberos, AllowAnoymous, out address))
            {
                Task<HttpResponseMessage> responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout);
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