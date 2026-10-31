// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Features.Authentication;
using Microsoft.AspNetCore.Shared;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Represents an implementation of the HTTP Context class.
/// </summary>
// DebuggerDisplayAttribute is inherited but we're replacing it on this implementation to include reason phrase.
[DebuggerDisplay("{DebuggerToString(),nq}")]
public sealed class DefaultHttpContext : HttpContext
{
    // The initial size of the feature collection when using the default constructor; based on number of common features
    // https://github.com/dotnet/aspnetcore/issues/31249
    private const int DefaultFeatureCollectionSize = 10;

    // Lambdas hoisted to static readonly fields to improve inlining https://github.com/dotnet/roslyn/issues/13624
    private static readonly Func<IFeatureCollection, IItemsFeature> _newItemsFeature = f => new ItemsFeature();
    private static readonly Func<DefaultHttpContext, IServiceProvidersFeature> _newServiceProvidersFeature = context => new RequestServicesFeature(context, context.ServiceScopeFactory);
    private static readonly Func<IFeatureCollection, IHttpAuthenticationFeature> _newHttpAuthenticationFeature = f => new HttpAuthenticationFeature();
    private static readonly Func<IFeatureCollection, IHttpRequestLifetimeFeature> _newHttpRequestLifetimeFeature = f => new HttpRequestLifetimeFeature();
    private static readonly Func<IFeatureCollection, ISessionFeature> _newSessionFeature = f => new DefaultSessionFeature();
    private static readonly Func<IFeatureCollection, ISessionFeature?> _nullSessionFeature = f => null;
    private static readonly Func<IFeatureCollection, IHttpRequestIdentifierFeature> _newHttpRequestIdentifierFeature = f => new HttpRequestIdentifierFeature();

    private FeatureReferences<FeatureInterfaces> _features;

    private readonly DefaultHttpRequest _request;
    private readonly DefaultHttpResponse _response;

    private DefaultConnectionInfo? _connection;
    private DefaultWebSocketManager? _websockets;

    // This is field exists to make analyzing memory dumps easier.
    // https://github.com/dotnet/aspnetcore/issues/29709
    internal bool _active;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultHttpContext"/> class.
    /// </summary>
    public DefaultHttpContext()
        : this(new FeatureCollection(DefaultFeatureCollectionSize))
    {
        Features.Set<IHttpRequestFeature>(new HttpRequestFeature());
        Features.Set<IHttpResponseFeature>(new HttpResponseFeature());
        Features.Set<IHttpResponseBodyFeature>(new StreamResponseBodyFeature(Stream.Null));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultHttpContext"/> class with provided features.
    /// </summary>
    /// <param name="features">Initial set of features for the <see cref="DefaultHttpContext"/>.</param>
    public DefaultHttpContext(IFeatureCollection features)
    {
        _features.Initalize(features);
        _request = new DefaultHttpRequest(this);
        _response = new DefaultHttpResponse(this);
    }

    /// <summary>
    /// Reinitialize  the current instant of the class with features passed in.
    /// </summary>
    /// <remarks>
    /// This method allows the consumer to re-use the <see cref="DefaultHttpContext" /> for another request, rather than having to allocate a new instance.
    /// </remarks>
    /// <param name="features">The new set of features for the <see cref="DefaultHttpContext" />.</param>
    public void Initialize(IFeatureCollection features)
    {
        var revision = features.Revision;
        _features.Initalize(features, revision);
        _request.Initialize(revision);
        _response.Initialize(revision);
        _connection?.Initialize(features, revision);
        _websockets?.Initialize(features, revision);
        _active = true;
    }

    /// <summary>
    /// Uninitialize all the features in the <see cref="DefaultHttpContext" />.
    /// </summary>
    public void Uninitialize()
    {
        _features = default;
        _request.Uninitialize();
        _response.Uninitialize();
        _connection?.Uninitialize();
        _websockets?.Uninitialize();
        _active = false;
    }

    /// <summary>
    /// Gets or set the <see cref="FormOptions" /> for this instance.
    /// </summary>
    /// <returns>
    /// <see cref="FormOptions"/>
    /// </returns>
    public FormOptions FormOptions { get; set; } = default!;

    /// <summary>
    /// Gets or sets the <see cref="IServiceScopeFactory" /> for this instance.
    /// </summary>
    /// <returns>
    /// <see cref="IServiceScopeFactory"/>
    /// </returns>
    public IServiceScopeFactory ServiceScopeFactory { get; set; } = default!;

    private IItemsFeature ItemsFeature =>
        _features.Fetch(ref _features.Cache.Items, _newItemsFeature)!;

    private IServiceProvidersFeature ServiceProvidersFeature =>
        _features.Fetch(ref _features.Cache.ServiceProviders, this, _newServiceProvidersFeature)!;

    private IHttpAuthenticationFeature HttpAuthenticationFeature =>
        _features.Fetch(ref _features.Cache.Authentication, _newHttpAuthenticationFeature)!;

    private IHttpRequestLifetimeFeature LifetimeFeature =>
        _features.Fetch(ref _features.Cache.Lifetime, _newHttpRequestLifetimeFeature)!;

    private ISessionFeature SessionFeature =>
        _features.Fetch(ref _features.Cache.Session, _newSessionFeature)!;

    private ISessionFeature? SessionFeatureOrNull =>
        _features.Fetch(ref _features.Cache.Session, _nullSessionFeature);

    private IHttpRequestIdentifierFeature RequestIdentifierFeature =>
        _features.Fetch(ref _features.Cache.RequestIdentifier, _newHttpRequestIdentifierFeature)!;

    /// <inheritdoc/>
    public override IFeatureCollection Features => _features.Collection ?? ContextDisposed();

    /// <inheritdoc/>
    public override HttpRequest Request => _request;

    /// <inheritdoc/>
    public override HttpResponse Response => _response;

    /// <inheritdoc/>
    public override ConnectionInfo Connection => _connection ?? (_connection = new DefaultConnectionInfo(Features));

    /// <inheritdoc/>
    public override WebSocketManager WebSockets => _websockets ?? (_websockets = new DefaultWebSocketManager(Features));

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public override IDictionary<object, object?> Items
    {
        get { return ItemsFeature.Items; }
        set { ItemsFeature.Items = value; }
    }

    /// <inheritdoc/>
    public override IServiceProvider RequestServices
    {
        get { return ServiceProvidersFeature.RequestServices; }
        set { ServiceProvidersFeature.RequestServices = value; }
    }

    /// <inheritdoc/>
    public override CancellationToken RequestAborted
    {
        get { return LifetimeFeature.RequestAborted; }
        set { LifetimeFeature.RequestAborted = value; }
    }

    /// <inheritdoc/>
    public override string TraceIdentifier
    {
        get { return RequestIdentifierFeature.TraceIdentifier; }
        set { RequestIdentifierFeature.TraceIdentifier = value; }
    }

    /// <inheritdoc/>
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
    /// <summary>
    /// This API is used by ASP.NET Core's infrastructure and should not be used by application code.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public HttpContext HttpContext => this;

    /// <inheritdoc/>
    public override void Abort()
    {
        LifetimeFeature.Abort();
    }

    private static IFeatureCollection ContextDisposed()
    {
        ThrowContextDisposed();
        return null;
    }

    [DoesNotReturn]
    private static void ThrowContextDisposed()
    {
        throw new ObjectDisposedException(nameof(HttpContext), $"Request has finished and {nameof(HttpContext)} disposed.");
    }

    private string DebuggerToString()
    {
        // DebuggerToString is also on this type because this project has access to ReasonPhrases.
        return HttpContextDebugFormatter.ContextToString(this, ReasonPhrases.GetReasonPhrase(Response.StatusCode));
    }

    struct FeatureInterfaces
    {
        public IItemsFeature? Items;
        public IServiceProvidersFeature? ServiceProviders;
        public IHttpAuthenticationFeature? Authentication;
        public IHttpRequestLifetimeFeature? Lifetime;
        public ISessionFeature? Session;
        public IHttpRequestIdentifierFeature? RequestIdentifier;
    }
}
