// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace InteropTests.Helpers
{
    public class InteropTestsFixture : IDisposable
    {
        private WebsiteProcess _process;

        public string Path { get; set; }


        public string ServerPort { get; private set; }


        public async Task EnsureStarted(ITestOutputHelper output)
        {
            if (_process != null)
            {
                return;
            }

            if (string.IsNullOrEmpty(Path))
            {
                throw new InvalidOperationException("Path has not been set.");
            }

            _process = new WebsiteProcess(Path, output);

            await _process.WaitForReady();

            ServerPort = _process.ServerPort;
        }

        public void Dispose()
        {
            _process.Dispose();
        }
    }
}
