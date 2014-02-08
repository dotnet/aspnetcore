// -----------------------------------------------------------------------
// <copyright file="DigestTests.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Authentication.ExtendedProtection;
using System.Threading.Tasks;
using Microsoft.AspNet.Server.WebListener;
using Xunit;

namespace Microsoft.AspNet.Security.Windows.Tests
{
    using AppFunc = Func<IDictionary<string, object>, Task>;

    public class DigestTests
    {
        private const string Address = "http://localhost:8080/";
        private const string SecureAddress = "https://localhost:9090/";
        private const int DefaultStatusCode = 201;

        [Fact]
        public async Task Digest_PartialMatch_PassedThrough()
        {
            WindowsAuthMiddleware windowsAuth = new WindowsAuthMiddleware(SimpleApp);
            IDictionary<string, object> emptyEnv = CreateEmptyRequest("Authorization", "Digestion blablabla");
            await windowsAuth.Invoke(emptyEnv);

            Assert.Equal(DefaultStatusCode, emptyEnv.Get<int>("owin.ResponseStatusCode"));
            var responseHeaders = emptyEnv.Get<IDictionary<string, string[]>>("owin.ResponseHeaders");
            Assert.Equal(0, responseHeaders.Count);
        }

        [Fact]
        public async Task Digest_BadData_400()
        {
            WindowsAuthMiddleware windowsAuth = new WindowsAuthMiddleware(SimpleApp);
            IDictionary<string, object> emptyEnv = CreateEmptyRequest("Authorization", "Digest blablabla");
            await windowsAuth.Invoke(emptyEnv);

            Assert.Equal(400, emptyEnv.Get<int>("owin.ResponseStatusCode"));
            var responseHeaders = emptyEnv.Get<IDictionary<string, string[]>>("owin.ResponseHeaders");
            Assert.Equal(0, responseHeaders.Count);
        }

        [Fact]
        public async Task Digest_AppSets401_401WithChallenge()
        {
            WindowsAuthMiddleware windowsAuth = new WindowsAuthMiddleware(new DenyAnonymous(SimpleApp).Invoke);
            windowsAuth.AuthenticationSchemes = AuthTypes.Digest;
            IDictionary<string, object> emptyEnv = CreateEmptyRequest();
            await windowsAuth.Invoke(emptyEnv);
            FireOnSendingHeadersActions(emptyEnv);

            Assert.Equal(401, emptyEnv.Get<int>("owin.ResponseStatusCode"));
            var responseHeaders = emptyEnv.Get<IDictionary<string, string[]>>("owin.ResponseHeaders");
            Assert.Equal(1, responseHeaders.Count);
            Assert.NotNull(responseHeaders.Get("www-authenticate"));
            Assert.True(responseHeaders.Get("www-authenticate").StartsWith("Digest "));
        }

        [Fact]
        public async Task Digest_CbtOptionalButNotPresent_401WithChallenge()
        {
            WindowsAuthMiddleware windowsAuth = new WindowsAuthMiddleware(new DenyAnonymous(SimpleApp).Invoke);
            windowsAuth.AuthenticationSchemes = AuthTypes.Digest;
            windowsAuth.ExtendedProtectionPolicy = new ExtendedProtectionPolicy(PolicyEnforcement.WhenSupported);
            IDictionary<string, object> emptyEnv = CreateEmptyRequest();
            emptyEnv["owin.RequestScheme"] = "https";
            await windowsAuth.Invoke(emptyEnv);
            FireOnSendingHeadersActions(emptyEnv);

            Assert.Equal(401, emptyEnv.Get<int>("owin.ResponseStatusCode"));
            var responseHeaders = emptyEnv.Get<IDictionary<string, string[]>>("owin.ResponseHeaders");
            Assert.Equal(0, responseHeaders.Count);
            Assert.Null(responseHeaders.Get("www-authenticate"));
        }

        [Fact]
        public async Task Digest_CbtRequiredButNotPresent_400()
        {
            WindowsAuthMiddleware windowsAuth = new WindowsAuthMiddleware(new DenyAnonymous(SimpleApp).Invoke);
            windowsAuth.AuthenticationSchemes = AuthTypes.Digest;
            windowsAuth.ExtendedProtectionPolicy = new ExtendedProtectionPolicy(PolicyEnforcement.Always);
            IDictionary<string, object> emptyEnv = CreateEmptyRequest();
            emptyEnv["owin.RequestScheme"] = "https";
            await windowsAuth.Invoke(emptyEnv);
            FireOnSendingHeadersActions(emptyEnv);

            Assert.Equal(401, emptyEnv.Get<int>("owin.ResponseStatusCode"));
            var responseHeaders = emptyEnv.Get<IDictionary<string, string[]>>("owin.ResponseHeaders");
            Assert.Equal(0, responseHeaders.Count);
            Assert.Null(responseHeaders.Get("www-authenticate"));
        }

        [Fact(Skip = "Broken")]
        public async Task Digest_ClientAuthenticates_Success()
        {
            WindowsAuthMiddleware windowsAuth = new WindowsAuthMiddleware(new DenyAnonymous(SimpleApp).Invoke);
            windowsAuth.AuthenticationSchemes = AuthTypes.Digest;
            
            using (CreateServer(windowsAuth.Invoke))
            {
                HttpResponseMessage response = await SendAuthRequestAsync(Address);
                Assert.Equal(DefaultStatusCode, (int)response.StatusCode);
            }
        }

        [Fact(Skip = "Broken")]
        public async Task Digest_ClientAuthenticatesMultipleTimes_Success()
        {
            WindowsAuthMiddleware windowsAuth = new WindowsAuthMiddleware(new DenyAnonymous(SimpleApp).Invoke);
            windowsAuth.AuthenticationSchemes = AuthTypes.Digest;

            using (CreateServer(windowsAuth.Invoke))
            {
                for (int i = 0; i < 10; i++)
                {
                    HttpResponseMessage response = await SendAuthRequestAsync(Address);
                    Assert.Equal(DefaultStatusCode, (int)response.StatusCode);
                }
            }
        }

        [Fact]
        public async Task Digest_AnonmousClient_401()
        {
            WindowsAuthMiddleware windowsAuth = new WindowsAuthMiddleware(new DenyAnonymous(SimpleApp).Invoke);
            windowsAuth.AuthenticationSchemes = AuthTypes.Digest;

            using (CreateServer(windowsAuth.Invoke))
            {
                HttpResponseMessage response = await SendRequestAsync(Address);
                Assert.Equal(401, (int)response.StatusCode);
                Assert.True(response.Headers.WwwAuthenticate.ToString().StartsWith("Digest "));
            }
        }

        [Fact(Skip = "Broken")]
        public async Task Digest_ClientAuthenticatesWithCbt_Success()
        {
            WindowsAuthMiddleware windowsAuth = new WindowsAuthMiddleware(new DenyAnonymous(SimpleApp).Invoke);
            windowsAuth.AuthenticationSchemes = AuthTypes.Digest;
            windowsAuth.ExtendedProtectionPolicy = new ExtendedProtectionPolicy(PolicyEnforcement.Always);

            using (CreateSecureServer(windowsAuth.Invoke))
            {
                HttpResponseMessage response = await SendAuthRequestAsync(SecureAddress);
                Assert.Equal(DefaultStatusCode, (int)response.StatusCode);
            }
        }

        private IDictionary<string, object> CreateEmptyRequest(string header = null, string value = null)
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
    }
}
