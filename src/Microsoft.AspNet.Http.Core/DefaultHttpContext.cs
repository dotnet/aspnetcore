// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.FeatureModel;
using Microsoft.AspNet.Http.Core.Collections;
using Microsoft.AspNet.Http.Core.Infrastructure;
using Microsoft.AspNet.Http.Core.Authentication;
using Microsoft.AspNet.Http.Infrastructure;
using Microsoft.AspNet.Http.Interfaces;
using Microsoft.AspNet.Http.Interfaces.Authentication;
using Microsoft.AspNet.Http.Authentication;

namespace Microsoft.AspNet.Http.Core
{
    public class DefaultHttpContext : HttpContext
    {
        private static IList<string> EmptyList = new List<string>();

        private readonly HttpRequest _request;
        private readonly HttpResponse _response;

        private FeatureReference<IItemsFeature> _items;
        private FeatureReference<IServiceProvidersFeature> _serviceProviders;
        private FeatureReference<IHttpAuthenticationFeature> _authentication;
        private FeatureReference<IHttpRequestLifetimeFeature> _lifetime;
        private FeatureReference<IHttpWebSocketFeature> _webSockets;
        private FeatureReference<ISessionFeature> _session;
        private IFeatureCollection _features;

        public DefaultHttpContext()
            : this(new FeatureCollection())
        {
            SetFeature<IHttpRequestFeature>(new HttpRequestFeature());
            SetFeature<IHttpResponseFeature>(new HttpResponseFeature());
        }

        public DefaultHttpContext(IFeatureCollection features)
        {
            _features = features;
            _request = new DefaultHttpRequest(this, features);
            _response = new DefaultHttpResponse(this, features);

            _items = FeatureReference<IItemsFeature>.Default;
            _serviceProviders = FeatureReference<IServiceProvidersFeature>.Default;
            _authentication = FeatureReference<IHttpAuthenticationFeature>.Default;
            _lifetime = FeatureReference<IHttpRequestLifetimeFeature>.Default;
            _webSockets = FeatureReference<IHttpWebSocketFeature>.Default;
            _session = FeatureReference<ISessionFeature>.Default;
        }

        IItemsFeature ItemsFeature
        {
            get { return _items.Fetch(_features) ?? _items.Update(_features, new ItemsFeature()); }
        }

        IServiceProvidersFeature ServiceProvidersFeature
        {
            get { return _serviceProviders.Fetch(_features) ?? _serviceProviders.Update(_features, new ServiceProvidersFeature()); }
        }

        private IHttpAuthenticationFeature HttpAuthenticationFeature
        {
            get { return _authentication.Fetch(_features) ?? _authentication.Update(_features, new HttpAuthenticationFeature()); }
        }

        private IHttpRequestLifetimeFeature LifetimeFeature
        {
            get { return _lifetime.Fetch(_features); }
        }

        private IHttpWebSocketFeature WebSocketFeature
        {
            get { return _webSockets.Fetch(_features); }
        }

        private ISessionFeature SessionFeature
        {
            get { return _session.Fetch(_features); }
        }

        public override HttpRequest Request { get { return _request; } }

        public override HttpResponse Response { get { return _response; } }

        public override ClaimsPrincipal User
        {
            get
            {
                var user = HttpAuthenticationFeature.User;
                if (user == null)
                {
                    user = new ClaimsPrincipal(new ClaimsIdentity());
                    HttpAuthenticationFeature.User = user;
                }
                return user;
            }
            set { HttpAuthenticationFeature.User = value; }
        }

        public override IDictionary<object, object> Items
        {
            get { return ItemsFeature.Items; }
        }

        public override IServiceProvider ApplicationServices
        {
            get { return ServiceProvidersFeature.ApplicationServices; }
            set { ServiceProvidersFeature.ApplicationServices = value; }
        }

        public override IServiceProvider RequestServices
        {
            get { return ServiceProvidersFeature.RequestServices; }
            set { ServiceProvidersFeature.RequestServices = value; }
        }

        public int Revision { get { return _features.Revision; } }

        public override CancellationToken RequestAborted
        {
            get
            {
                var lifetime = LifetimeFeature;
                if (lifetime != null)
                {
                    return lifetime.RequestAborted;
                }
                return CancellationToken.None;
            }
        }

        public override ISessionCollection Session
        {
            get
            {
                var feature = SessionFeature;
                if (feature == null)
                {
                    throw new InvalidOperationException("Session has not been configured for this application or request.");
                }
                if (feature.Session == null)
                {
                    if (feature.Factory == null)
                    {
                        throw new InvalidOperationException("No ISessionFactory available to create the ISession.");
                    }
                    feature.Session = feature.Factory.Create();
                }
                return new SessionCollection(feature.Session);
            }
        }

        public override bool IsWebSocketRequest
        {
            get
            {
                var webSocketFeature = WebSocketFeature;
                return webSocketFeature != null && webSocketFeature.IsWebSocketRequest;
            }
        }

        public override IList<string> WebSocketRequestedProtocols
        {
            get
            {
                return Request.Headers.GetValues(Constants.Headers.WebSocketSubProtocols) ?? EmptyList;
            }
        }

        public override void Abort()
        {
            var lifetime = LifetimeFeature;
            if (lifetime != null)
            {
                lifetime.Abort();
            }
        }

        public override void Dispose()
        {
            // REVIEW: is this necessary? is the environment "owned" by the context?
            _features.Dispose();
        }

        public override object GetFeature(Type type)
        {
            object value;
            return _features.TryGetValue(type, out value) ? value : null;
        }

        public override void SetFeature(Type type, object instance)
        {
            _features[type] = instance;
        }

        public override IEnumerable<AuthenticationDescription> GetAuthenticationSchemes()
        {
            var handler = HttpAuthenticationFeature.Handler;
            if (handler == null)
            {
                return new AuthenticationDescription[0];
            }

            var describeContext = new DescribeSchemesContext();
            handler.GetDescriptions(describeContext);
            return describeContext.Results;
        }

        public override IEnumerable<AuthenticationResult> Authenticate([NotNull] IEnumerable<string> authenticationSchemes)
        {
            var handler = HttpAuthenticationFeature.Handler;

            var authenticateContext = new AuthenticateContext(authenticationSchemes);
            if (handler != null)
            {
                handler.Authenticate(authenticateContext);
            }

            // Verify all types ack'd
            IEnumerable<string> leftovers = authenticationSchemes.Except(authenticateContext.Accepted);
            if (leftovers.Any())
            {
                throw new InvalidOperationException("The following authentication schemes were not accepted: " + string.Join(", ", leftovers));
            }

            return authenticateContext.Results;
        }

        public override async Task<IEnumerable<AuthenticationResult>> AuthenticateAsync([NotNull] IEnumerable<string> authenticationSchemes)
        {
            var handler = HttpAuthenticationFeature.Handler;

            var authenticateContext = new AuthenticateContext(authenticationSchemes);
            if (handler != null)
            {
                await handler.AuthenticateAsync(authenticateContext);
            }

            // Verify all types ack'd
            IEnumerable<string> leftovers = authenticationSchemes.Except(authenticateContext.Accepted);
            if (leftovers.Any())
            {
                throw new InvalidOperationException("The following authentication schemes were not accepted: " + string.Join(", ", leftovers));
            }

            return authenticateContext.Results;
        }

        public override Task<WebSocket> AcceptWebSocketAsync(string subProtocol)
        {
            var webSocketFeature = WebSocketFeature;
            if (WebSocketFeature == null)
            {
                throw new NotSupportedException("WebSockets are not supported");
            }
            return WebSocketFeature.AcceptAsync(new WebSocketAcceptContext() { SubProtocol = subProtocol } );
        }
    }
}