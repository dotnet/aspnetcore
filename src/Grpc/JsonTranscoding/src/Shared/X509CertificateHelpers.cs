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

using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace Grpc.Shared;

internal static class X509CertificateHelpers
{
    internal const string X509SubjectAlternativeNameId = "2.5.29.17";
    internal const string X509SubjectAlternativeNameKey = "x509_subject_alternative_name";
    internal const string X509CommonNameKey = "x509_common_name";

    public static string[] GetDnsFromExtensions(X509Certificate2 cert)
    {
        foreach (X509Extension ext in cert.Extensions)
        {
            if (ext.Oid?.Value == X509SubjectAlternativeNameId)
            {
                var subjectAlternativeName = new X509SubjectAlternativeNameExtension(ext.RawData, ext.Critical);
                return subjectAlternativeName.EnumerateDnsNames().ToArray();
            }
        }

        return Array.Empty<string>();
    }
}
