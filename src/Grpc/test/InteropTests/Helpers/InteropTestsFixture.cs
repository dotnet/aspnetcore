// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace InteropTests.Helpers
{
    public class InteropTestsFixture : IDisposable
    {
        private WebServerProcess _process;

        public async Task EnsureStarted(ITestOutputHelper output)
        {
            if (_process != null)
            {
                return;
            }

            var webPath = @"C:\Development\Source\AspNetCore\src\Grpc\test\testassets\InteropTestsWebsite\";

            _process = new WebServerProcess(webPath, output);

            await _process.WaitForReady();
        }

        public void Dispose()
        {
            _process.Dispose();
        }
    }
}
