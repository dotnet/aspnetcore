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
    public class DefaultHttpContext : HttpContext, IFeatureCache
    {
        private readonly HttpRequest _request;
        private readonly HttpResponse _response;
        private readonly ConnectionInfo _connection;
        private readonly AuthenticationManager _authenticationManager;

        private IItemsFeature _items;
        private IServiceProvidersFeature _serviceProviders;
        private IHttpAuthenticationFeature _authentication;
        private IHttpRequestLifetimeFeature _lifetime;
        private ISessionFeature _session;
        private WebSocketManager _websockets;

        private IFeatureCollection _features;
        private int _cachedFeaturesRevision = -1;

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
        }

        void IFeatureCache.CheckFeaturesRevision()
        {
            if (_cachedFeaturesRevision !=_features.Revision)
            {
                _items = null;
                _serviceProviders = null;
                _authentication = null;
                _lifetime = null;
                _session = null;
                _cachedFeaturesRevision = _features.Revision;
            }
        }

        IItemsFeature ItemsFeature
        {
            get
            {
                return FeatureHelpers.GetOrCreateAndCache(
                    this, 
                    _features, 
                    () => new ItemsFeature(), 
                    ref _items);
            }
        }

        IServiceProvidersFeature ServiceProvidersFeature
        {
            get
            {
                return FeatureHelpers.GetOrCreateAndCache(
                    this, 
                    _features, 
                    () => new ServiceProvidersFeature(), 
                    ref _serviceProviders);
            }
        }

        private IHttpAuthenticationFeature HttpAuthenticationFeature
        {
            get
            {
                return FeatureHelpers.GetOrCreateAndCache(
                    this, 
                    _features, 
                    () => new HttpAuthenticationFeature(), 
                    ref _authentication);
            }
        }

        private IHttpRequestLifetimeFeature LifetimeFeature
        {
            get
            {
                return FeatureHelpers.GetOrCreateAndCache(
                    this, 
                    _features, 
                    () => new HttpRequestLifetimeFeature(), 
                    ref _lifetime);
            }
        }

        private ISessionFeature SessionFeature
        {
            get { return FeatureHelpers.GetAndCache(this, _features, ref _session); }
            set
            {
                _features.Set(value);
                _session = value;
            }
        }

        private IHttpRequestIdentifierFeature RequestIdentifierFeature
        {
            get {
                return FeatureHelpers.GetOrCreate<IHttpRequestIdentifierFeature>(
                  _features,
                  () => new HttpRequestIdentifierFeature());
            }
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

        public override string TraceIdentifier
        {
            get { return RequestIdentifierFeature.TraceIdentifier; }
            set { RequestIdentifierFeature.TraceIdentifier = value; }
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
                    SessionFeature = feature;
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
    }
}