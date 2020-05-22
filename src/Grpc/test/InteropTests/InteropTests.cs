// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using InteropTests.Helpers;
using Microsoft.AspNetCore.Testing;
using Xunit;
using Xunit.Abstractions;

namespace InteropTests
{
    public class InteropTests
    {
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);
        private readonly string _clientPath = Path.Combine(Directory.GetCurrentDirectory(), "InteropClient", "InteropClient.dll");
        private readonly string _serverPath = Path.Combine(Directory.GetCurrentDirectory(), "InteropWebsite", "InteropWebsite.dll");
        private readonly ITestOutputHelper _output;

        public InteropTests(ITestOutputHelper output)
        {
            _output = output;
        }

        // All interop test cases, minus GCE authentication specific tests
        [ConditionalTheory]
        [InlineData("empty_unary")]
        [InlineData("large_unary")]
        [InlineData("client_streaming")]
        [InlineData("server_streaming")]
        [InlineData("ping_pong", Skip = "https://github.com/dotnet/aspnetcore/issues/22101")]
        [InlineData("empty_stream")]
        [InlineData("cancel_after_begin")]
        [InlineData("cancel_after_first_response")]
        [InlineData("timeout_on_sleeping_server")]
        [InlineData("custom_metadata")]
        [InlineData("status_code_and_message")]
        [InlineData("special_status_message")]
        [InlineData("unimplemented_service")]
        [InlineData("unimplemented_method")]
        [InlineData("client_compressed_unary")]
        [InlineData("client_compressed_streaming")]
        [InlineData("server_compressed_unary")]
        [InlineData("server_compressed_streaming")]
        public async Task InteropTestCase(string name)
        {
            using (var serverProcess = new WebsiteProcess(_serverPath, _output))
            {
                await serverProcess.WaitForReady().TimeoutAfter(DefaultTimeout);

                using (var clientProcess = new ClientProcess(_output, _clientPath, serverProcess.ServerPort, name))
                {
                    await clientProcess.WaitForReady().TimeoutAfter(DefaultTimeout);

                    await clientProcess.Exited.TimeoutAfter(DefaultTimeout);

                    Assert.Equal(0, clientProcess.ExitCode);
                }
            }
        }
    }
}
