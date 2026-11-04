// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Http;

internal sealed class DefaultConnectionInfo : ConnectionInfo
{
    // Lambdas hoisted to static readonly fields to improve inlining https://github.com/dotnet/roslyn/issues/13624
    private static readonly Func<IFeatureCollection, IHttpConnectionFeature> _newHttpConnectionFeature = f => new HttpConnectionFeature();
    private static readonly Func<IFeatureCollection, ITlsConnectionFeature> _newTlsConnectionFeature = f => new TlsConnectionFeature();
    private static readonly Func<IFeatureCollection, IConnectionLifetimeNotificationFeature> _newConnectionLifetime = f => new DefaultConnectionLifetimeNotificationFeature(f.Get<IHttpResponseFeature>());

    private FeatureReferences<FeatureInterfaces> _features;

    public DefaultConnectionInfo(IFeatureCollection features)
    {
        Initialize(features);
    }

    public void Initialize(IFeatureCollection features)
    {
        _features.Initalize(features);
    }

    public void Initialize(IFeatureCollection features, int revision)
    {
        _features.Initalize(features, revision);
    }

    public void Uninitialize()
    {
        _features = default;
    }

    private IHttpConnectionFeature HttpConnectionFeature =>
        _features.Fetch(ref _features.Cache.Connection, _newHttpConnectionFeature)!;

    private ITlsConnectionFeature TlsConnectionFeature =>
        _features.Fetch(ref _features.Cache.TlsConnection, _newTlsConnectionFeature)!;

    private IConnectionLifetimeNotificationFeature ConnectionLifetime =>
        _features.Fetch(ref _features.Cache.ConnectionLifetime, _newConnectionLifetime)!;

    /// <inheritdoc />
    public override string Id
    {
        get { return HttpConnectionFeature.ConnectionId; }
        set { HttpConnectionFeature.ConnectionId = value; }
    }

    public override IPAddress? RemoteIpAddress
    {
        get { return HttpConnectionFeature.RemoteIpAddress; }
        set { HttpConnectionFeature.RemoteIpAddress = value; }
    }

    public override int RemotePort
    {
        get { return HttpConnectionFeature.RemotePort; }
        set { HttpConnectionFeature.RemotePort = value; }
    }

    public override IPAddress? LocalIpAddress
    {
        get { return HttpConnectionFeature.LocalIpAddress; }
        set { HttpConnectionFeature.LocalIpAddress = value; }
    }

    public override int LocalPort
    {
        get { return HttpConnectionFeature.LocalPort; }
        set { HttpConnectionFeature.LocalPort = value; }
    }

    public override X509Certificate2? ClientCertificate
    {
        get { return TlsConnectionFeature.ClientCertificate; }
        set { TlsConnectionFeature.ClientCertificate = value; }
    }

    public override Task<X509Certificate2?> GetClientCertificateAsync(CancellationToken cancellationToken = default)
    {
        return TlsConnectionFeature.GetClientCertificateAsync(cancellationToken);
    }

    public override void RequestClose()
    {
        ConnectionLifetime.RequestClose();
    }

    struct FeatureInterfaces
    {
        public IHttpConnectionFeature? Connection;
        public ITlsConnectionFeature? TlsConnection;
        public IConnectionLifetimeNotificationFeature? ConnectionLifetime;
    }
}
