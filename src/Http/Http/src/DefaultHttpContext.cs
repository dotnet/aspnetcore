// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Security.Claims;
using System.Threading;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Features.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Http
{
    public sealed class DefaultHttpContext : HttpContext
    {
        // Lambdas hoisted to static readonly fields to improve inlining https://github.com/dotnet/roslyn/issues/13624
        private readonly static Func<IFeatureCollection, IItemsFeature> _newItemsFeature = f => new ItemsFeature();
        private readonly static Func<DefaultHttpContext, IServiceProvidersFeature> _newServiceProvidersFeature = context => new RequestServicesFeature(context, context.ServiceScopeFactory);
        private readonly static Func<IFeatureCollection, IHttpAuthenticationFeature> _newHttpAuthenticationFeature = f => new HttpAuthenticationFeature();
        private readonly static Func<IFeatureCollection, IHttpRequestLifetimeFeature> _newHttpRequestLifetimeFeature = f => new HttpRequestLifetimeFeature();
        private readonly static Func<IFeatureCollection, ISessionFeature> _newSessionFeature = f => new DefaultSessionFeature();
        private readonly static Func<IFeatureCollection, ISessionFeature> _nullSessionFeature = f => null;
        private readonly static Func<IFeatureCollection, IHttpRequestIdentifierFeature> _newHttpRequestIdentifierFeature = f => new HttpRequestIdentifierFeature();

        private FeatureReferences<FeatureInterfaces> _features;

        private readonly DefaultHttpRequest _request;
        private readonly DefaultHttpResponse _response;

        private DefaultConnectionInfo _connection;
        private DefaultWebSocketManager _websockets;

        public DefaultHttpContext()
            : this(new FeatureCollection())
        {
            Features.Set<IHttpRequestFeature>(new HttpRequestFeature());
            Features.Set<IHttpResponseFeature>(new HttpResponseFeature());
            Features.Set<IHttpResponseBodyFeature>(new StreamResponseBodyFeature(Stream.Null));
        }

        public DefaultHttpContext(IFeatureCollection features)
        {
            _features.Initalize(features);
            _request = new DefaultHttpRequest(this);
            _response = new DefaultHttpResponse(this);
        }

        public void Initialize(IFeatureCollection features)
        {
            var revision = features.Revision;
            _features.Initalize(features, revision);
            _request.Initialize(revision);
            _response.Initialize(revision);
            _connection?.Initialize(features, revision);
            _websockets?.Initialize(features, revision);
        }

        public void Uninitialize()
        {
            _features = default;
            _request.Uninitialize();
            _response.Uninitialize();
            _connection?.Uninitialize();
            _websockets?.Uninitialize();
        }

        public FormOptions FormOptions { get; set; }

        public IServiceScopeFactory ServiceScopeFactory { get; set; }

        private IItemsFeature ItemsFeature =>
            _features.Fetch(ref _features.Cache.Items, _newItemsFeature);

        private IServiceProvidersFeature ServiceProvidersFeature =>
            _features.Fetch(ref _features.Cache.ServiceProviders, this, _newServiceProvidersFeature);

        private IHttpAuthenticationFeature HttpAuthenticationFeature =>
            _features.Fetch(ref _features.Cache.Authentication, _newHttpAuthenticationFeature);

        private IHttpRequestLifetimeFeature LifetimeFeature =>
            _features.Fetch(ref _features.Cache.Lifetime, _newHttpRequestLifetimeFeature);

        private ISessionFeature SessionFeature =>
            _features.Fetch(ref _features.Cache.Session, _newSessionFeature);

        private ISessionFeature SessionFeatureOrNull =>
            _features.Fetch(ref _features.Cache.Session, _nullSessionFeature);


        private IHttpRequestIdentifierFeature RequestIdentifierFeature =>
            _features.Fetch(ref _features.Cache.RequestIdentifier, _newHttpRequestIdentifierFeature);

        public override IFeatureCollection Features => _features.Collection ?? ContextDisposed();

        public override HttpRequest Request => _request;

        public override HttpResponse Response => _response;

        public override ConnectionInfo Connection => _connection ?? (_connection = new DefaultConnectionInfo(Features));

        public override WebSocketManager WebSockets => _websockets ?? (_websockets = new DefaultWebSocketManager(Features));

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

        // This property exists because of backwards compatibility.
        // We send an anonymous object with an HttpContext property
        // via DiagnosticListener in various events throughout the pipeline. Instead
        // we just send the HttpContext to avoid extra allocations
        [EditorBrowsable(EditorBrowsableState.Never)]
        public HttpContext HttpContext => this;

        public override void Abort()
        {
            LifetimeFeature.Abort();
        }

        private static IFeatureCollection ContextDisposed()
        {
            ThrowContextDisposed();
            return null;
        }

        private static void ThrowContextDisposed()
        {
            throw new ObjectDisposedException(nameof(HttpContext), $"Request has finished and {nameof(HttpContext)} disposed.");
        }

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
