// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using Microsoft.AspNetCore.Http.Authentication;
using Microsoft.AspNetCore.Http.Authentication.Internal;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Features.Authentication;
using Microsoft.AspNetCore.Http.Features.Authentication.Internal;
using Microsoft.AspNetCore.Http.Features.Internal;

namespace Microsoft.AspNetCore.Http.Internal
{
    public class DefaultHttpContext : HttpContext
    {
        private FeatureReferences<FeatureInterfaces> _features;

        private HttpRequest _request;
        private HttpResponse _response;
        private AuthenticationManager _authenticationManager;
        private ConnectionInfo _connection;
        private WebSocketManager _websockets;

        public DefaultHttpContext()
            : this(new FeatureCollection())
        {
            Features.Set<IHttpRequestFeature>(new HttpRequestFeature());
            Features.Set<IHttpResponseFeature>(new HttpResponseFeature());
        }

        public DefaultHttpContext(IFeatureCollection features)
        {
            Initialize(features);
        }

        public virtual void Initialize(IFeatureCollection features)
        {
            _features = new FeatureReferences<FeatureInterfaces>(features);
            _request = InitializeHttpRequest();
            _response = InitializeHttpResponse();
        }

        public virtual void Uninitialize()
        {
            _features = default(FeatureReferences<FeatureInterfaces>);
            if (_request != null)
            {
                UninitializeHttpRequest(_request);
                _request = null;
            }
            if (_response != null)
            {
                UninitializeHttpResponse(_response);
                _response = null;
            }
            if (_authenticationManager != null)
            {
                UninitializeAuthenticationManager(_authenticationManager);
                _authenticationManager = null;
            }
            if (_connection != null)
            {
                UninitializeConnectionInfo(_connection);
                _connection = null;
            }
            if (_websockets != null)
            {
                UninitializeWebSocketManager(_websockets);
                _websockets = null;
            }
        }
        
        private IItemsFeature ItemsFeature =>
            _features.Fetch(ref _features.Cache.Items, f => new ItemsFeature());

        private IServiceProvidersFeature ServiceProvidersFeature =>
            _features.Fetch(ref _features.Cache.ServiceProviders, f => new ServiceProvidersFeature());

        private IHttpAuthenticationFeature HttpAuthenticationFeature =>
            _features.Fetch(ref _features.Cache.Authentication, f => new HttpAuthenticationFeature());

        private IHttpRequestLifetimeFeature LifetimeFeature =>
            _features.Fetch(ref _features.Cache.Lifetime, f => new HttpRequestLifetimeFeature());

        private ISessionFeature SessionFeature =>
            _features.Fetch(ref _features.Cache.Session, f => new DefaultSessionFeature());

        private ISessionFeature SessionFeatureOrNull =>
            _features.Fetch(ref _features.Cache.Session, f => null);


        private IHttpRequestIdentifierFeature RequestIdentifierFeature =>
            _features.Fetch(ref _features.Cache.RequestIdentifier, f => new HttpRequestIdentifierFeature());

        public override IFeatureCollection Features => _features.Collection;

        public override HttpRequest Request => _request;

        public override HttpResponse Response => _response;

        public override ConnectionInfo Connection => _connection ?? (_connection = InitializeConnectionInfo());

        public override AuthenticationManager Authentication => _authenticationManager ?? (_authenticationManager = InitializeAuthenticationManager());

        public override WebSocketManager WebSockets => _websockets ?? (_websockets = InitializeWebSocketManager());


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
                var feature = SessionFeatureOrNull;
                if (feature == null)
                {
                    throw new InvalidOperationException("Session has not been configured for this application " +
                        "or request.");
                }
                return feature.Session;
            }
            set
            {
                SessionFeature.Session = value;
            }
        }

        

        public override void Abort()
        {
            LifetimeFeature.Abort();
        }


        protected virtual HttpRequest InitializeHttpRequest() => new DefaultHttpRequest(this);
        protected virtual void UninitializeHttpRequest(HttpRequest instance) { }

        protected virtual HttpResponse InitializeHttpResponse() => new DefaultHttpResponse(this);
        protected virtual void UninitializeHttpResponse(HttpResponse instance) { }

        protected virtual ConnectionInfo InitializeConnectionInfo() => new DefaultConnectionInfo(Features);
        protected virtual void UninitializeConnectionInfo(ConnectionInfo instance) { }

        protected virtual AuthenticationManager InitializeAuthenticationManager() => new DefaultAuthenticationManager(this);
        protected virtual void UninitializeAuthenticationManager(AuthenticationManager instance) { }

        protected virtual WebSocketManager InitializeWebSocketManager() => new DefaultWebSocketManager(Features);
        protected virtual void UninitializeWebSocketManager(WebSocketManager instance) { }

        struct FeatureInterfaces
        {
            public IItemsFeature Items;
            public IServiceProvidersFeature ServiceProviders;
            public IHttpAuthenticationFeature Authentication;
            public IHttpRequestLifetimeFeature Lifetime;
            public ISessionFeature Session;
            public IHttpRequestIdentifierFeature RequestIdentifier;
        }
    }
}
