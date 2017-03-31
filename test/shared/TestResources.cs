// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;

namespace Microsoft.AspNetCore.Testing
{
    public static class TestResources
    {
        private static readonly string _testCertificatePath =
#if NET46
            Path.Combine(Directory.GetCurrentDirectory(), "testCert.pfx");
#elif NETCOREAPP2_0
            Path.Combine(AppContext.BaseDirectory, "testCert.pfx");
#else
#error Target framework needs to be updated
#endif

        public static string TestCertificatePath => _testCertificatePath;
    }
}