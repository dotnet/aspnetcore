// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Security;
using Microsoft.AspNet.HttpFeature;
using Xunit;

namespace Microsoft.AspNet.Owin
{
    public class OwinEnvironmentTests
    {
        private T Get<T>(IDictionary<string, object> environment, string key)
        {
            object value;
            return environment.TryGetValue(key, out value) ? (T)value : default(T);
        }

        [Fact]
        public void OwinEnvironmentCanBeCreated()
        {
            MoqHttpContext context = new MoqHttpContext();
            context.Request.Method = "SomeMethod";
            IDictionary<string, object> env = new OwinEnvironment(context);

            Assert.Equal("SomeMethod", Get<string>(env, "owin.RequestMethod"));
            env["owin.RequestMethod"] = "SomeOtherMethod";
            Assert.Equal("SomeOtherMethod", context.Request.Method);
        }

        private class MoqHttpContext : HttpContext
        {
            private HttpRequest _request;
            private IDictionary<object, object> _items;

            public MoqHttpContext()
            {
                _request = new MoqHttpRequest();
                _items = new Dictionary<object, object>();
            }

            public override HttpRequest Request
            {
                get { return _request; }
            }

            public override HttpResponse Response
            {
                get { throw new NotImplementedException(); }
            }

            public override ClaimsPrincipal User
            {
                get { throw new NotImplementedException(); }
                set { throw new NotImplementedException(); }
            }

            public override IDictionary<object, object> Items
            {
                get { return _items; }
            }

            public override IServiceProvider ApplicationServices
            {
                get { throw new NotImplementedException(); }
                set { throw new NotImplementedException(); }
            }

            public override IServiceProvider RequestServices
            {
                get { throw new NotImplementedException(); }
                set { throw new NotImplementedException(); }
            }

            public override void Dispose()
            {
                throw new NotImplementedException();
            }

            public override object GetFeature(Type type)
            {
                return Request;
            }

            public override void SetFeature(Type type, object instance)
            {
                throw new NotImplementedException();
            }

            public override IEnumerable<AuthenticationDescription> GetAuthenticationTypes()
            {
                throw new NotImplementedException();
            }

            public override IEnumerable<AuthenticationResult> Authenticate(IList<string> authenticationTypes)
            {
                throw new NotImplementedException();
            }

            public override Task<IEnumerable<AuthenticationResult>> AuthenticateAsync(IList<string> authenticationTypes)
            {
                throw new NotImplementedException();
            }

            public override CancellationToken OnRequestAborted
            {
                get { throw new NotImplementedException(); }
            }

            public override bool IsWebSocketRequest
            {
                get { throw new NotImplementedException(); }
            }

            public override IList<string> WebSocketRequestedProtocols
            {
                get { throw new NotImplementedException(); }
            }

            public override void Abort()
            {
                throw new NotImplementedException();
            }

            public override Task<WebSocket> AcceptWebSocketAsync(string subProtocol)
            {
                throw new NotImplementedException();
            }
        }

        private class MoqHttpRequest : HttpRequest, IHttpRequestFeature
        {
            public override HttpContext HttpContext
            {
                get { throw new NotImplementedException(); }
            }

            public override string Method
            {
                get;
                set;
            }

            public override string Scheme
            {
                get;
                set;
            }

            public override bool IsSecure
            {
                get { return false; }
            }

            public override HostString Host
            {
                get { throw new NotImplementedException(); }
                set { throw new NotImplementedException(); }
            }

            public override PathString PathBase
            {
                get { throw new NotImplementedException(); }
                set { throw new NotImplementedException(); }
            }

            public override PathString Path
            {
                get { throw new NotImplementedException(); }
                set { throw new NotImplementedException(); }
            }

            public override QueryString QueryString
            {
                get { throw new NotImplementedException(); }
                set { throw new NotImplementedException(); }
            }

            public override IReadableStringCollection Query
            {
                get { throw new NotImplementedException(); }
            }

            public override Task<IReadableStringCollection> GetFormAsync()
            {
                throw new NotImplementedException();
            }

            public override string Protocol
            {
                get { throw new NotImplementedException(); }
                set { throw new NotImplementedException(); }
            }

            public override IHeaderDictionary Headers
            {
                get { throw new NotImplementedException(); }
            }

            public override IReadableStringCollection Cookies
            {
                get { throw new NotImplementedException(); }
            }

            public override long? ContentLength
            {
                get { throw new NotImplementedException(); }
                set { throw new NotImplementedException(); }
            }

            public override Stream Body
            {
                get { throw new NotImplementedException(); }
                set { throw new NotImplementedException(); }
            }

            public override CancellationToken CallCanceled
            {
                get { throw new NotImplementedException(); }
                set { throw new NotImplementedException(); }
            }

            string IHttpRequestFeature.PathBase
            {
                get;
                set;
            }

            string IHttpRequestFeature.Path
            {
                get;
                set;
            }

            string IHttpRequestFeature.QueryString
            {
                get;
                set;
            }

            IDictionary<string, string[]> IHttpRequestFeature.Headers
            {
                get;
                set;
            }
        }
    }
}
