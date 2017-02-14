// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;

namespace Microsoft.AspNetCore.Testing
{
    public static class TestResources
    {
        private static readonly string _testCertificatePath =
#if NET452
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "testCert.pfx");
#else
            Path.Combine(AppContext.BaseDirectory, "testCert.pfx");
#endif

        public static string TestCertificatePath => _testCertificatePath;
    }
}