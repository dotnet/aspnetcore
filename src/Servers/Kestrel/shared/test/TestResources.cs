// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Xunit;

namespace Microsoft.AspNetCore.Testing
{
    public static class TestResources
    {
        private static readonly string _baseDir = Path.Combine(Directory.GetCurrentDirectory(), "shared", "TestCertificates");

        public static string TestCertificatePath { get; } = Path.Combine(_baseDir, "testCert.pfx");
        public static string GetCertPath(string name) => Path.Combine(_baseDir, name);

        private const int MutexTimeout = 120 * 1000;
        private static readonly Mutex importPfxMutex = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
            new Mutex(initiallyOwned: false, "Global\\KestrelTests.Certificates.LoadPfxCertificate") :
            null;

        public static X509Certificate2 GetTestCertificate(string certName = "testCert.pfx")
        {
            // On Windows, applications should not import PFX files in parallel to avoid a known system-level
            // race condition bug in native code which can cause crashes/corruption of the certificate state.
            if (importPfxMutex != null)
            {
                Assert.True(importPfxMutex.WaitOne(MutexTimeout), "Cannot acquire the global certificate mutex.");
            }

            try
            {
                return new X509Certificate2(GetCertPath(certName), "testPassword");
            }
            finally
            {
                importPfxMutex?.ReleaseMutex();
            }
        }

        public static X509Certificate2 GetTestCertificate(string certName, string password)
        {
            return new X509Certificate2(GetCertPath(certName), password);
        }
    }
}
