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

#pragma warning disable CA1810 // Initialize all static fields inline.

using System.Globalization;
using System.Security.Cryptography.X509Certificates;

namespace Grpc.Shared;

internal static class X509CertificateHelpers
{
    internal const string X509SubjectAlternativeNameId = "2.5.29.17";
    internal const string X509SubjectAlternativeNameKey = "x509_subject_alternative_name";
    internal const string X509CommonNameKey = "x509_common_name";

    // From https://github.com/dotnet/wcf/blob/08371d989fc1d230dbe9520916644f7a7f5dc954/src/System.Private.ServiceModel/src/System/IdentityModel/Claims/X509CertificateClaimSet.cs#L297-L332
    public static string[] GetDnsFromExtensions(X509Certificate2 cert)
    {
        foreach (X509Extension ext in cert.Extensions)
        {
            // Extension is SAN2
            if (ext.Oid?.Value == X509SubjectAlternativeNameConstants.Oid)
            {
                string asnString = ext.Format(false);
                if (string.IsNullOrWhiteSpace(asnString))
                {
                    return Array.Empty<string>();
                }

                // SubjectAlternativeNames might contain something other than a dNSName,
                // so we have to parse through and only use the dNSNames
                // <identifier><delimter><value><separator(s)>

                string[] rawDnsEntries =
                    asnString.Split(new string[1] { X509SubjectAlternativeNameConstants.Separator }, StringSplitOptions.RemoveEmptyEntries);

                List<string> dnsEntries = new List<string>();

                for (var i = 0; i < rawDnsEntries.Length; i++)
                {
                    string[] keyval = rawDnsEntries[i].Split(X509SubjectAlternativeNameConstants.Delimiter);
                    if (string.Equals(keyval[0], X509SubjectAlternativeNameConstants.Identifier, StringComparison.Ordinal))
                    {
                        dnsEntries.Add(keyval[1]);
                    }
                }

                return dnsEntries.ToArray();
            }
        }
        return Array.Empty<string>();
    }

    // We don't have a strongly typed extension to parse Subject Alt Names, so we have to do a workaround
    // to figure out what the identifier, delimiter, and separator is by using a well-known extension
    private static class X509SubjectAlternativeNameConstants
    {
        public const string Oid = "2.5.29.17";

        private static readonly string? s_identifier;
        private static readonly char s_delimiter;
        private static readonly string? s_separator;

        private static readonly bool s_successfullyInitialized;
        private static readonly Exception? s_initializationException;

        public static string Identifier
        {
            get
            {
                EnsureInitialized();
                return s_identifier!;
            }
        }

        public static char Delimiter
        {
            get
            {
                EnsureInitialized();
                return s_delimiter;
            }
        }
        public static string Separator
        {
            get
            {
                EnsureInitialized();
                return s_separator!;
            }
        }

        private static void EnsureInitialized()
        {
            if (!s_successfullyInitialized)
            {
                throw new FormatException(string.Format(
                    CultureInfo.InvariantCulture,
                    "There was an error detecting the identifier, delimiter, and separator for X509CertificateClaims on this platform.{0}" +
                    "Detected values were: Identifier: '{1}'; Delimiter:'{2}'; Separator:'{3}'",
                    Environment.NewLine,
                    s_identifier,
                    s_delimiter,
                    s_separator
                ), s_initializationException);
            }
        }

        // static initializer runs only when one of the properties is accessed
        static X509SubjectAlternativeNameConstants()
        {
            // Extracted a well-known X509Extension
            byte[] x509ExtensionBytes = new byte[] {
                    48, 36, 130, 21, 110, 111, 116, 45, 114, 101, 97, 108, 45, 115, 117, 98, 106, 101, 99,
                    116, 45, 110, 97, 109, 101, 130, 11, 101, 120, 97, 109, 112, 108, 101, 46, 99, 111, 109
                };
            const string subjectName1 = "not-real-subject-name";

            try
            {
                X509Extension x509Extension = new X509Extension(Oid, x509ExtensionBytes, true);
                string x509ExtensionFormattedString = x509Extension.Format(false);

                // Each OS has a different dNSName identifier and delimiter
                // On Windows, dNSName == "DNS Name" (localizable), on Linux, dNSName == "DNS"
                // e.g.,
                // Windows: x509ExtensionFormattedString is: "DNS Name=not-real-subject-name, DNS Name=example.com"
                // Linux:   x509ExtensionFormattedString is: "DNS:not-real-subject-name, DNS:example.com"
                // Parse: <identifier><delimter><value><separator(s)>

                int delimiterIndex = x509ExtensionFormattedString.IndexOf(subjectName1, StringComparison.Ordinal) - 1;
                s_delimiter = x509ExtensionFormattedString[delimiterIndex];

                // Make an assumption that all characters from the the start of string to the delimiter
                // are part of the identifier
                s_identifier = x509ExtensionFormattedString.Substring(0, delimiterIndex);

                int separatorFirstChar = delimiterIndex + subjectName1.Length + 1;
                int separatorLength = 1;
                for (var i = separatorFirstChar + 1; i < x509ExtensionFormattedString.Length; i++)
                {
                    // We advance until the first character of the identifier to determine what the
                    // separator is. This assumes that the identifier assumption above is correct
                    if (x509ExtensionFormattedString[i] == s_identifier[0])
                    {
                        break;
                    }

                    separatorLength++;
                }

                s_separator = x509ExtensionFormattedString.Substring(separatorFirstChar, separatorLength);

                s_successfullyInitialized = true;
            }
            catch (Exception ex)
            {
                s_successfullyInitialized = false;
                s_initializationException = ex;
            }
        }
    }
}
