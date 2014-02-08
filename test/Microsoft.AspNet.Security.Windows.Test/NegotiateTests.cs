// -----------------------------------------------------------------------
// <copyright file="NegotiateTests.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Authentication.ExtendedProtection;
using System.Threading.Tasks;
using Microsoft.AspNet.Server.WebListener;
using Xunit;
using Xunit.Extensions;

namespace Microsoft.AspNet.Security.Windows.Tests
{
    using AppFunc = Func<IDictionary<string, object>, Task>;

    public class NegotiateTests
    {
        private const string Address = "http://localhost:8080/";
        private const string SecureAddress = "https://localhost:9090/";
        private const int DefaultStatusCode = 201;
        
        [Theory]
        [InlineData("Negotiate")]
        [InlineData("NTLM")]
        public async Task Negotiate_PartialMatch_PassedThrough(string package)
        {
            WindowsAuthMiddleware windowsAuth = new WindowsAuthMiddleware(SimpleApp);
            IDictionary<string, object> emptyEnv = CreateEmptyRequest("Authorization", package + "ion blablabla");
            await windowsAuth.Invoke(emptyEnv);

            Assert.Equal(DefaultStatusCode, emptyEnv.Get<int>("owin.ResponseStatusCode"));
            var responseHeaders = emptyEnv.Get<IDictionary<string, string[]>>("owin.ResponseHeaders");
            Assert.Equal(0, responseHeaders.Count);
        }

        [Theory]
        [InlineData("Negotiate")]
        [InlineData("NTLM")]
        public async Task Negotiate_BadData_400(string package)
        {
            WindowsAuthMiddleware windowsAuth = new WindowsAuthMiddleware(SimpleApp);
            IDictionary<string, object> emptyEnv = CreateEmptyRequest("Authorization", package + " blablabla");
            await windowsAuth.Invoke(emptyEnv);

            Assert.Equal(400, emptyEnv.Get<int>("owin.ResponseStatusCode"));
            var responseHeaders = emptyEnv.Get<IDictionary<string, string[]>>("owin.ResponseHeaders");
            Assert.Equal(0, responseHeaders.Count);
        }

        [Theory]
        [InlineData("Negotiate")]
        [InlineData("NTLM")]
        public async Task Negotiate_AppSets401_401WithChallenge(string package)
        {
            WindowsAuthMiddleware windowsAuth = new WindowsAuthMiddleware(SimpleApp401);
            windowsAuth.AuthenticationSchemes = (AuthTypes)Enum.Parse(typeof(AuthTypes), package, true);
            IDictionary<string, object> emptyEnv = CreateEmptyRequest();
            await windowsAuth.Invoke(emptyEnv);
            FireOnSendingHeadersActions(emptyEnv);

            Assert.Equal(401, emptyEnv.Get<int>("owin.ResponseStatusCode"));
            var responseHeaders = emptyEnv.Get<IDictionary<string, string[]>>("owin.ResponseHeaders");
            Assert.Equal(1, responseHeaders.Count);
            Assert.NotNull(responseHeaders.Get("www-authenticate"));
            Assert.Equal(package, responseHeaders.Get("www-authenticate"));
        }

        [Theory(Skip = "Broken")]
        [InlineData("Negotiate")]
        [InlineData("NTLM")]
        public async Task Negotiate_ClientAuthenticates_Success(string package)
        {
            WindowsAuthMiddleware windowsAuth = new WindowsAuthMiddleware(new DenyAnonymous(SimpleApp).Invoke);
            windowsAuth.AuthenticationSchemes = (AuthTypes)Enum.Parse(typeof(AuthTypes), package, true);

            using (CreateServer(windowsAuth.Invoke))
            {
                HttpResponseMessage response = await SendAuthRequestAsync(Address);
                Assert.Equal(DefaultStatusCode, (int)response.StatusCode);
            }
        }

        [Theory(Skip = "Broken")]
        [InlineData("Negotiate")]
        [InlineData("NTLM")]
        public async Task Negotiate_ClientAuthenticatesMultipleTimes_Success(string package)
        {
            WindowsAuthMiddleware windowsAuth = new WindowsAuthMiddleware(new DenyAnonymous(SimpleApp).Invoke);
            windowsAuth.AuthenticationSchemes = (AuthTypes)Enum.Parse(typeof(AuthTypes), package, true);

            using (CreateServer(windowsAuth.Invoke))
            {
                for (int i = 0; i < 10; i++)
                {
                    HttpResponseMessage response = await SendAuthRequestAsync(Address);
                    Assert.Equal(DefaultStatusCode, (int)response.StatusCode);
                }
            }
        }

        [Theory]
        [InlineData("Negotiate")]
        [InlineData("NTLM")]
        public async Task Negotiate_AnonmousClient_401(string package)
        {
            WindowsAuthMiddleware windowsAuth = new WindowsAuthMiddleware(new DenyAnonymous(SimpleApp).Invoke);
            windowsAuth.AuthenticationSchemes = (AuthTypes)Enum.Parse(typeof(AuthTypes), package, true);

            using (CreateServer(windowsAuth.Invoke))
            {
                HttpResponseMessage response = await SendRequestAsync(Address);
                Assert.Equal(401, (int)response.StatusCode);
                Assert.Equal(package, response.Headers.WwwAuthenticate.ToString());
            }
        }

        [Fact(Skip = "Broken")]
        public async Task UnsafeSharedNTLM_AuthenticatedClient_Success()
        {
            WindowsAuthMiddleware windowsAuth = new WindowsAuthMiddleware(new DenyAnonymous(SimpleApp).Invoke);
            windowsAuth.AuthenticationSchemes = AuthTypes.Ntlm;
            windowsAuth.UnsafeConnectionNtlmAuthentication = true;

            using (CreateServer(windowsAuth.Invoke))
            {
                WebRequestHandler handler = new WebRequestHandler();
                CredentialCache cache = new CredentialCache();
                cache.Add(new Uri(Address), "NTLM", CredentialCache.DefaultNetworkCredentials);
                handler.Credentials = cache;
                handler.UnsafeAuthenticatedConnectionSharing = true;
                using (HttpClient client = new HttpClient(handler))
                {
                    HttpResponseMessage response = await client.GetAsync(Address);
                    Assert.Equal(DefaultStatusCode, (int)response.StatusCode);
                    response.EnsureSuccessStatusCode();

                    // Remove the credentials before try two just to prove they aren't used.
                    cache.Remove(new Uri(Address), "NTLM");
                    response = await client.GetAsync(Address);
                    Assert.Equal(DefaultStatusCode, (int)response.StatusCode);
                }
            }
        }

        [Theory(Skip = "Broken")]
        [InlineData("Negotiate")]
        [InlineData("NTLM")]
        public async Task Negotiate_ClientAuthenticatesWithCbt_Success(string package)
        {
            WindowsAuthMiddleware windowsAuth = new WindowsAuthMiddleware(new DenyAnonymous(SimpleApp).Invoke);
            windowsAuth.AuthenticationSchemes = (AuthTypes)Enum.Parse(typeof(AuthTypes), package, true);
            windowsAuth.ExtendedProtectionPolicy = new ExtendedProtectionPolicy(PolicyEnforcement.Always);

            using (CreateSecureServer(windowsAuth.Invoke))
            {
                HttpResponseMessage response = await SendAuthRequestAsync(SecureAddress);
                Assert.Equal(DefaultStatusCode, (int)response.StatusCode);
            }
        }

        private IDictionary<string, object> CreateEmptyRequest(string header = null, string value = null, string connectionId = "Random")
        {
            IDictionary<string, object> env = new Dictionary<string, object>();
            var requestHeaders = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
            env["owin.RequestHeaders"] = requestHeaders;
            if (header != null)
            {
                requestHeaders[header] = new string[] { value };
            }
            env["owin.ResponseHeaders"] = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

            var onSendingHeadersActions = new List<Tuple<Action<object>, object>>();
            env["server.OnSendingHeaders"] = new Action<Action<object>, object>(
                (a, b) => onSendingHeadersActions.Add(new Tuple<Action<object>, object>(a, b)));

            env["test.OnSendingHeadersActions"] = onSendingHeadersActions;
            env["server.ConnectionId"] = connectionId;
            return env;
        }

        private void FireOnSendingHeadersActions(IDictionary<string, object> env)
        {
            var onSendingHeadersActions = env.Get<IList<Tuple<Action<object>, object>>>("test.OnSendingHeadersActions");
            foreach (var actionPair in onSendingHeadersActions.Reverse())
            {
                actionPair.Item1(actionPair.Item2);
            }
        }

        private IDisposable CreateServer(AppFunc app)
        {
            IDictionary<string, object> properties = new Dictionary<string, object>();
            IList<IDictionary<string, object>> addresses = new List<IDictionary<string, object>>();
            properties["host.Addresses"] = addresses;

            IDictionary<string, object> address = new Dictionary<string, object>();
            addresses.Add(address);

            address["scheme"] = "http";
            address["host"] = "localhost";
            address["port"] = "8080";
            address["path"] = string.Empty;

            return OwinServerFactory.Create(app, properties);
        }

        private IDisposable CreateSecureServer(AppFunc app)
        {
            IDictionary<string, object> properties = new Dictionary<string, object>();
            IList<IDictionary<string, object>> addresses = new List<IDictionary<string, object>>();
            properties["host.Addresses"] = addresses;

            IDictionary<string, object> address = new Dictionary<string, object>();
            addresses.Add(address);

            address["scheme"] = "https";
            address["host"] = "localhost";
            address["port"] = "9090";
            address["path"] = string.Empty;

            return OwinServerFactory.Create(app, properties);
        }

        private async Task<HttpResponseMessage> SendRequestAsync(string uri)
        {
            using (HttpClient client = new HttpClient())
            {
                return await client.GetAsync(uri);
            }
        }

        private async Task<HttpResponseMessage> SendAuthRequestAsync(string uri)
        {
            WebRequestHandler handler = new WebRequestHandler();
            handler.UseDefaultCredentials = true;
            handler.ServerCertificateValidationCallback = (a, b, c, d) => true;
            using (HttpClient client = new HttpClient(handler))
            {
                return await client.GetAsync(uri);
            }
        }

        private Task SimpleApp(IDictionary<string, object> env)
        {
            env["owin.ResponseStatusCode"] = DefaultStatusCode;
            return Task.FromResult<object>(null);
        }

        private Task SimpleApp401(IDictionary<string, object> env)
        {
            env["owin.ResponseStatusCode"] = 401;
            return Task.FromResult<object>(null);
        }
    }
}
