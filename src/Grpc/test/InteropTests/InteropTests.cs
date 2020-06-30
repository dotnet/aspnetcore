// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using InteropTests.Helpers;
using Microsoft.AspNetCore.Testing;
using Xunit;
using Xunit.Abstractions;

namespace InteropTests
{
    // All interop test cases, minus GCE authentication specific tests.
    // Tests are separate methods so that they can be quarantined separately.
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

        [Fact]
        public Task EmptyUnary() => InteropTestCase("empty_unary");

        [Fact]
        [QuarantinedTest]
        public Task LargeUnary() => InteropTestCase("large_unary");

        [Fact]
        [QuarantinedTest]
        public Task ClientStreaming() => InteropTestCase("client_streaming");

        [Fact]
        public Task ServerStreaming() => InteropTestCase("server_streaming");

        [Fact]
        [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/22101")]
        public Task PingPong() => InteropTestCase("ping_pong");

        [Fact]
        public Task EmptyStream() => InteropTestCase("empty_stream");

        [Fact]
        public Task CancelAfterBegin() => InteropTestCase("cancel_after_begin");

        [Fact]
        public Task CancelAfterFirstResponse() => InteropTestCase("cancel_after_first_response");

        [Fact]
        public Task TimeoutOnSleepingServer() => InteropTestCase("timeout_on_sleeping_server");

        [Fact]
        [QuarantinedTest]
        public Task CustomMetadata() => InteropTestCase("custom_metadata");

        [Fact]
        public Task StatusCodeAndMessage() => InteropTestCase("status_code_and_message");

        [Fact]
        public Task SpecialStatusMessage() => InteropTestCase("special_status_message");

        [Fact]
        public Task UnimplementedService() => InteropTestCase("unimplemented_service");

        [Fact]
        public Task UnimplementedMethod() => InteropTestCase("unimplemented_method");

        [Fact]
        [QuarantinedTest]
        public Task ClientCompressedUnary() => InteropTestCase("client_compressed_unary");

        [Fact]
        public Task ClientCompressedStreaming() => InteropTestCase("client_compressed_streaming");

        [Fact]
        [QuarantinedTest]
        public Task ServerCompressedUnary() => InteropTestCase("server_compressed_unary");

        [Fact]
        public Task ServerCompressedStreaming() => InteropTestCase("server_compressed_streaming");

        private async Task InteropTestCase(string name)
        {
            using (var serverProcess = new WebsiteProcess(_serverPath, _output))
            {
                await serverProcess.WaitForReady().TimeoutAfter(DefaultTimeout);

                using (var clientProcess = new ClientProcess(_output, _clientPath, serverProcess.ServerPort, name))
                {
                    try
                    {
                        await clientProcess.WaitForReadyAsync().TimeoutAfter(DefaultTimeout);

                        await clientProcess.WaitForExitAsync().TimeoutAfter(DefaultTimeout);

                        Assert.Equal(0, clientProcess.ExitCode);
                    }
                    catch (Exception ex)
                    {
                        var errorMessage = $@"Error while running client process.

Server ready: {serverProcess.IsReady}
Client ready: {clientProcess.IsReady}

Server process output:
======================================
{serverProcess.GetOutput()}
======================================

Client process output:
======================================
{clientProcess.GetOutput()}
======================================";
                        throw new InvalidOperationException(errorMessage, ex);
                    }
                }
            }
        }
    }
}
