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
    public class DefaultConnectionInfo : ConnectionInfo
    {
        private FeatureReferences<FeatureInterfaces> _features;

        public DefaultConnectionInfo(IFeatureCollection features)
        {
            Initialize(features);
        }

        public virtual void Initialize( IFeatureCollection features)
        {
            _features = new FeatureReferences<FeatureInterfaces>(features);
        }

        public virtual void Uninitialize()
        {
            _features = default(FeatureReferences<FeatureInterfaces>);
        }

        private IHttpConnectionFeature HttpConnectionFeature =>
            _features.Fetch(ref _features.Cache.Connection, f => new HttpConnectionFeature());

        private ITlsConnectionFeature TlsConnectionFeature=>
            _features.Fetch(ref _features.Cache.TlsConnection, f => new TlsConnectionFeature());

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

        struct FeatureInterfaces
        {
            public IHttpConnectionFeature Connection;
            public ITlsConnectionFeature TlsConnection;
        }
    }
}