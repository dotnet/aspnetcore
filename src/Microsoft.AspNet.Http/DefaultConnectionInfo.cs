// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Http.Features;
using Microsoft.AspNet.Http.Features.Internal;

namespace Microsoft.AspNet.Http.Internal
{
    public class DefaultConnectionInfo : ConnectionInfo, IFeatureCache
    {
        private readonly IFeatureCollection _features;
        private int _cachedFeaturesRevision = -1;

        private IHttpConnectionFeature _connection;
        private ITlsConnectionFeature _tlsConnection;

        public DefaultConnectionInfo(IFeatureCollection features)
        {
            _features = features;
        }

        void IFeatureCache.CheckFeaturesRevision()
        {
            if (_cachedFeaturesRevision != _features.Revision)
            {
                _connection = null;
                _tlsConnection = null;
                _cachedFeaturesRevision = _features.Revision;
            }
        }

        private IHttpConnectionFeature HttpConnectionFeature
        {
            get
            {
                return FeatureHelpers.GetOrCreateAndCache(
                    this, 
                    _features, 
                    () => new HttpConnectionFeature(), 
                    ref _connection);
            }
        }

        private ITlsConnectionFeature TlsConnectionFeature
        {
            get
            {
                return FeatureHelpers.GetOrCreateAndCache(
                    this, 
                    _features,
                    () => new TlsConnectionFeature(),
                    ref _tlsConnection);
            }
        }

        public override IPAddress RemoteIpAddress
        {
            get { return HttpConnectionFeature.RemoteIpAddress; }
            set { HttpConnectionFeature.RemoteIpAddress = value; }
        }

        public override int RemotePort
        {
            get { return HttpConnectionFeature.RemotePort; }
            set { HttpConnectionFeature.RemotePort = value; }
        }

        public override IPAddress LocalIpAddress
        {
            get { return HttpConnectionFeature.LocalIpAddress; }
            set { HttpConnectionFeature.LocalIpAddress = value; }
        }

        public override int LocalPort
        {
            get { return HttpConnectionFeature.LocalPort; }
            set { HttpConnectionFeature.LocalPort = value; }
        }

        public override bool IsLocal
        {
            get { return HttpConnectionFeature.IsLocal; }
            set { HttpConnectionFeature.IsLocal = value; }
        }

        public override X509Certificate2 ClientCertificate
        {
            get { return TlsConnectionFeature.ClientCertificate; }
            set { TlsConnectionFeature.ClientCertificate = value; }
        }

        public override Task<X509Certificate2> GetClientCertificateAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            return TlsConnectionFeature.GetClientCertificateAsync(cancellationToken);
        }
    }
}