// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.FeatureModel;
using Microsoft.AspNet.Http.Features;
using Microsoft.AspNet.Http.Features.Internal;

namespace Microsoft.AspNet.Http.Internal
{
    public class DefaultConnectionInfo : ConnectionInfo
    {
        private readonly IFeatureCollection _features;

        private FeatureReference<IHttpConnectionFeature> _connection = FeatureReference<IHttpConnectionFeature>.Default;
        private FeatureReference<ITlsConnectionFeature> _tlsConnection = FeatureReference<ITlsConnectionFeature>.Default;

        public DefaultConnectionInfo(IFeatureCollection features)
        {
            _features = features;
        }

        private IHttpConnectionFeature HttpConnectionFeature
        {
            get { return _connection.Fetch(_features) ?? _connection.Update(_features, new HttpConnectionFeature()); }
        }

        private ITlsConnectionFeature TlsConnectionFeature
        {
            get { return _tlsConnection.Fetch(_features) ?? _tlsConnection.Update(_features, new TlsConnectionFeature()); }
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