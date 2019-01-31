// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.AspNetCore.Testing
{
    public static class TestResources
    {
        private static readonly string _baseDir = Path.Combine(Directory.GetCurrentDirectory(), "shared", "TestCertificates");

        public static string TestCertificatePath { get; } = Path.Combine(_baseDir, "testCert.pfx");
        public static string GetCertPath(string name) => Path.Combine(_baseDir, name);

        public static X509Certificate2 GetTestCertificate()
        {
            return new X509Certificate2(TestCertificatePath, "testPassword");
        }

        public static X509Certificate2 GetTestCertificate(string certName)
        {
            return new X509Certificate2(GetCertPath(certName), "testPassword");
        }
    }
}
