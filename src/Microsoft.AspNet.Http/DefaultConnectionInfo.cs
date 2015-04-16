// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.FeatureModel;
using Microsoft.AspNet.Http.Infrastructure;

namespace Microsoft.AspNet.Http
{
    public class DefaultConnectionInfo : ConnectionInfo
    {
        private readonly IFeatureCollection _features;

        private FeatureReference<IHttpConnectionFeature> _connection = FeatureReference<IHttpConnectionFeature>.Default;
        private FeatureReference<IHttpClientCertificateFeature> _clientCertificate = FeatureReference<IHttpClientCertificateFeature>.Default;

        public DefaultConnectionInfo(IFeatureCollection features)
        {
            _features = features;
        }

        private IHttpConnectionFeature HttpConnectionFeature
        {
            get { return _connection.Fetch(_features) ?? _connection.Update(_features, new HttpConnectionFeature()); }
        }

        private IHttpClientCertificateFeature HttpClientCertificateFeature
        {
            get { return _clientCertificate.Fetch(_features) ?? _clientCertificate.Update(_features, new HttpClientCertificateFeature()); }
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

        public override X509Certificate ClientCertificate
        {
            get { return HttpClientCertificateFeature.ClientCertificate; }
            set { HttpClientCertificateFeature.ClientCertificate = value; }
        }

        public override Task<X509Certificate> GetClientCertificateAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            return HttpClientCertificateFeature.GetClientCertificateAsync(cancellationToken);
        }
    }
}