// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;

namespace Microsoft.AspNetCore.Testing
{
    public static class TestResources
    {
        private static readonly string _baseDir = Directory.GetCurrentDirectory();

        public static string TestCertificatePath { get; } = Path.Combine(_baseDir, "testCert.pfx");
        public static string GetCertPath(string name) => Path.Combine(_baseDir, name);
    }
}
