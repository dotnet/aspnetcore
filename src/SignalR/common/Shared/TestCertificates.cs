// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Win32;

namespace Microsoft.AspNetCore.SignalR.Test.Internal;

internal static class TestCertificateHelper
{
    internal static X509Certificate2 GetTestCert()
    {
        bool useRSA = false;
        if (OperatingSystem.IsWindows())
        {
            // Detect Win10+
            var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            var major = key.GetValue("CurrentMajorVersionNumber") as int?;
            var minor = key.GetValue("CurrentMinorVersionNumber") as int?;

            if (major.HasValue && minor.HasValue)
            {
                useRSA = true;
            }
        }
        else
        {
            useRSA = true;
        }

        if (useRSA)
        {
            // RSA cert, won't work on Windows 8.1 & Windows 2012 R2 using HTTP2, and ECC won't work in some Node environments
            var certPath = Path.Combine(Path.GetDirectoryName(Assembly.GetCallingAssembly().Location), "TestCertificates", "testCert.pfx");
            return new X509Certificate2(certPath, "testPassword");
        }
        else
        {
            // ECC cert, works on Windows 8.1 & Windows 2012 R2 using HTTP2
            var certPath = Path.Combine(Path.GetDirectoryName(Assembly.GetCallingAssembly().Location), "TestCertificates", "testCertECC.pfx");
            return new X509Certificate2(certPath, "testPassword");
        }
    }
}
