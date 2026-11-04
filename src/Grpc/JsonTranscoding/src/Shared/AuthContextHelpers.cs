#region Copyright notice and license

// Copyright 2019 The gRPC Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#endregion

using System.Security.Cryptography.X509Certificates;
using System.Text;
using Grpc.Core;

namespace Grpc.Shared;

internal static class AuthContextHelpers
{
    public static AuthContext CreateAuthContext(X509Certificate2 clientCertificate)
    {
        // Map X509Certificate2 values to AuthContext. The name/values come BoringSSL via C Core
        // https://github.com/grpc/grpc/blob/a3cc5361e6f6eb679ccf5c36ecc6d0ca41b64f4f/src/core/lib/security/security_connector/ssl_utils.cc#L206-L248

        var properties = new Dictionary<string, List<AuthProperty>>(StringComparer.Ordinal);

        string? peerIdentityPropertyName = null;

        var dnsNames = X509CertificateHelpers.GetDnsFromExtensions(clientCertificate);
        foreach (var dnsName in dnsNames)
        {
            AddProperty(properties, X509CertificateHelpers.X509SubjectAlternativeNameKey, dnsName);

            if (peerIdentityPropertyName == null)
            {
                peerIdentityPropertyName = X509CertificateHelpers.X509SubjectAlternativeNameKey;
            }
        }

        var commonName = clientCertificate.GetNameInfo(X509NameType.SimpleName, false);
        if (commonName != null)
        {
            AddProperty(properties, X509CertificateHelpers.X509CommonNameKey, commonName);
            if (peerIdentityPropertyName == null)
            {
                peerIdentityPropertyName = X509CertificateHelpers.X509CommonNameKey;
            }
        }

        return new AuthContext(peerIdentityPropertyName, properties);

        static void AddProperty(Dictionary<string, List<AuthProperty>> properties, string name, string value)
        {
            if (!properties.TryGetValue(name, out var values))
            {
                values = new List<AuthProperty>();
                properties[name] = values;
            }

            values.Add(AuthProperty.Create(name, Encoding.UTF8.GetBytes(value)));
        }
    }

}
