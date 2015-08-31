// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using Microsoft.AspNet.Http.Authentication;
using Microsoft.AspNet.Http.Authentication.Internal;
using Microsoft.AspNet.Http.Features;
using Microsoft.AspNet.Http.Features.Authentication;
using Microsoft.AspNet.Http.Features.Authentication.Internal;
using Microsoft.AspNet.Http.Features.Internal;

namespace Microsoft.AspNet.Http.Internal
{
    public class DefaultHttpContext : HttpContext
    {
        private readonly HttpRequest _request;
        private readonly HttpResponse _response;
        private readonly ConnectionInfo _connection;
        private readonly AuthenticationManager _authenticationManager;

        private FeatureReference<IItemsFeature> _items;
        private FeatureReference<IServiceProvidersFeature> _serviceProviders;
        private FeatureReference<IHttpAuthenticationFeature> _authentication;
        private FeatureReference<IHttpRequestLifetimeFeature> _lifetime;
        private FeatureReference<ISessionFeature> _session;
        private WebSocketManager _websockets;
        private IFeatureCollection _features;

        public DefaultHttpContext()
            : this(new FeatureCollection())
        {
            _features.Set<IHttpRequestFeature>(new HttpRequestFeature());
            _features.Set<IHttpResponseFeature>(new HttpResponseFeature());
        }

        public DefaultHttpContext(IFeatureCollection features)
        {
            _features = features;
            _request = new DefaultHttpRequest(this, features);
            _response = new DefaultHttpResponse(this, features);
            _connection = new DefaultConnectionInfo(features);
            _authenticationManager = new DefaultAuthenticationManager(features);

            _items = FeatureReference<IItemsFeature>.Default;
            _serviceProviders = FeatureReference<IServiceProvidersFeature>.Default;
            _authentication = FeatureReference<IHttpAuthenticationFeature>.Default;
            _lifetime = FeatureReference<IHttpRequestLifetimeFeature>.Default;
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
            get { return _lifetime.Fetch(_features) ?? _lifetime.Update(_features, new HttpRequestLifetimeFeature()); }
        }

        private ISessionFeature SessionFeature
        {
            get { return _session.Fetch(_features); }
        }

        public override IFeatureCollection Features { get { return _features; } }

        public override HttpRequest Request { get { return _request; } }

        public override HttpResponse Response { get { return _response; } }

        public override ConnectionInfo Connection { get { return _connection; } }

        public override AuthenticationManager Authentication { get { return _authenticationManager; } }

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
            set { ItemsFeature.Items = value; }
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

        public override CancellationToken RequestAborted
        {
            get { return LifetimeFeature.RequestAborted; }
            set { LifetimeFeature.RequestAborted = value; }
        }

        public override ISession Session
        {
            get
            {
                var feature = SessionFeature;
                if (feature == null)
                {
                    throw new InvalidOperationException("Session has not been configured for this application " +
                        "or request.");
                }
                return feature.Session;
            }
            set
            {
                var feature = SessionFeature;
                if (feature == null)
                {
                    feature = new DefaultSessionFeature();
                    _session.Update(_features, feature);
                }
                feature.Session = value;
            }
        }

        public override WebSocketManager WebSockets
        {
            get
            {
                if (_websockets == null)
                {
                    _websockets = new DefaultWebSocketManager(_features);
                }
                return _websockets;
            }
        }

        public override void Abort()
        {
            LifetimeFeature.Abort();
        }

        public override void Dispose()
        {
            // REVIEW: is this necessary? is the environment "owned" by the context?
            _features.Dispose();
        }

        public override object GetFeature(Type type)
        {
            return _features[type];
        }

        public override void SetFeature(Type type, object instance)
        {
            _features[type] = instance;
        }
    }
}