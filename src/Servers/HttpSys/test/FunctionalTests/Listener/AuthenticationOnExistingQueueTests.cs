// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.HttpSys.Listener
{
    public class AuthenticationOnExistingQueueTests
    {
        private static readonly bool AllowAnoymous = true;
        private static readonly bool DenyAnoymous = false;

        [ConditionalTheory]
        [InlineData(AuthenticationSchemes.None)]
        [InlineData(AuthenticationSchemes.Negotiate)]
        [InlineData(AuthenticationSchemes.NTLM)]
        // [InlineData(AuthenticationSchemes.Digest)]
        [InlineData(AuthenticationSchemes.Basic)]
        [InlineData(AuthenticationSchemes.Negotiate | AuthenticationSchemes.NTLM | /*AuthenticationSchemes.Digest |*/ AuthenticationSchemes.Basic)]
        public async Task AuthTypes_AllowAnonymous_NoChallenge(AuthenticationSchemes authType)
        {
            using var baseServer = Utilities.CreateHttpAuthServer(authType, AllowAnoymous, out var address);
            using var server = Utilities.CreateServerOnExistingQueue(authType, AllowAnoymous, baseServer.Options.RequestQueueName);
            
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

        [ConditionalTheory]
        [InlineData(AuthenticationSchemes.Negotiate)]
        [InlineData(AuthenticationSchemes.NTLM)]
        // [InlineData(AuthenticationType.Digest)] // TODO: Not implemented
        [InlineData(AuthenticationSchemes.Basic)]
        public async Task AuthType_RequireAuth_ChallengesAdded(AuthenticationSchemes authType)
        {
            using var baseServer = Utilities.CreateHttpAuthServer(authType, DenyAnoymous, out var address);
            using var server = Utilities.CreateServerOnExistingQueue(authType, DenyAnoymous, baseServer.Options.RequestQueueName);

            Task<HttpResponseMessage> responseTask = SendRequestAsync(address);

            var contextTask = server.AcceptAsync(Utilities.DefaultTimeout); // Fails when the server shuts down, the challenge happens internally.
            var response = await responseTask;
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Equal(authType.ToString(), response.Headers.WwwAuthenticate.ToString(), StringComparer.OrdinalIgnoreCase);
        }

        [ConditionalTheory]
        [InlineData(AuthenticationSchemes.Negotiate)]
        [InlineData(AuthenticationSchemes.NTLM)]
        // [InlineData(AuthenticationSchemes.Digest)] // TODO: Not implemented
        [InlineData(AuthenticationSchemes.Basic)]
        public async Task AuthType_AllowAnonymousButSpecify401_ChallengesAdded(AuthenticationSchemes authType)
        {
            using var baseServer = Utilities.CreateHttpAuthServer(authType, AllowAnoymous, out var address);
            using var server = Utilities.CreateServerOnExistingQueue(authType, AllowAnoymous, baseServer.Options.RequestQueueName);
            
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

        [ConditionalFact]
        public async Task MultipleAuthTypes_AllowAnonymousButSpecify401_ChallengesAdded()
        {
            AuthenticationSchemes authType =
                AuthenticationSchemes.Negotiate
                | AuthenticationSchemes.NTLM
                /* | AuthenticationSchemes.Digest TODO: Not implemented */
                | AuthenticationSchemes.Basic;
            using var baseServer = Utilities.CreateHttpAuthServer(authType, AllowAnoymous, out var address);
            using var server = Utilities.CreateServerOnExistingQueue(authType, AllowAnoymous, baseServer.Options.RequestQueueName);
            
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

        [ConditionalTheory]
        [InlineData(AuthenticationSchemes.Negotiate)]
        [InlineData(AuthenticationSchemes.NTLM)]
        // [InlineData(AuthenticationSchemes.Digest)] // TODO: Not implemented
        // [InlineData(AuthenticationSchemes.Basic)] // Doesn't work with default creds
        [InlineData(AuthenticationSchemes.Negotiate | AuthenticationSchemes.NTLM | /*AuthenticationType.Digest |*/ AuthenticationSchemes.Basic)]
        public async Task AuthTypes_AllowAnonymousButSpecify401_Success(AuthenticationSchemes authType)
        {
            using var baseServer = Utilities.CreateHttpAuthServer(authType, AllowAnoymous, out var address);
            using var server = Utilities.CreateServerOnExistingQueue(authType, AllowAnoymous, baseServer.Options.RequestQueueName);
            
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

        [ConditionalTheory]
        [InlineData(AuthenticationSchemes.Negotiate)]
        [InlineData(AuthenticationSchemes.NTLM)]
        // [InlineData(AuthenticationSchemes.Digest)] // TODO: Not implemented
        // [InlineData(AuthenticationSchemes.Basic)] // Doesn't work with default creds
        [InlineData(AuthenticationSchemes.Negotiate | AuthenticationSchemes.NTLM | /*AuthenticationType.Digest |*/ AuthenticationSchemes.Basic)]
        public async Task AuthTypes_RequireAuth_Success(AuthenticationSchemes authType)
        {
            using var baseServer = Utilities.CreateHttpAuthServer(authType, DenyAnoymous, out var address);
            using var server = Utilities.CreateServerOnExistingQueue(authType, DenyAnoymous, baseServer.Options.RequestQueueName);

            Task<HttpResponseMessage> responseTask = SendRequestAsync(address, useDefaultCredentials: true);

            var context = await server.AcceptAsync(Utilities.DefaultTimeout);
            Assert.NotNull(context.User);
            Assert.True(context.User.Identity.IsAuthenticated);
            Assert.Equal(authType, context.Response.AuthenticationChallenges);
            context.Dispose();

            var response = await responseTask;
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        private async Task<HttpResponseMessage> SendRequestAsync(string uri, bool useDefaultCredentials = false)
        {
            HttpClientHandler handler = new HttpClientHandler();
            handler.UseDefaultCredentials = useDefaultCredentials;
            using HttpClient client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(5) };
            return await client.GetAsync(uri);
        }
    }
}
