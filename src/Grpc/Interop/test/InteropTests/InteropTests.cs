// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using InteropTests.Helpers;
using Microsoft.AspNetCore.InternalTesting;
using Xunit.Abstractions;

namespace InteropTests;

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
    public Task LargeUnary() => InteropTestCase("large_unary");

    [Fact]
    public Task ClientStreaming() => InteropTestCase("client_streaming");

    [Fact]
    public Task ServerStreaming() => InteropTestCase("server_streaming");

    [Fact]
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
    public Task ClientCompressedUnary() => InteropTestCase("client_compressed_unary");

    [Fact]
    public Task ClientCompressedStreaming() => InteropTestCase("client_compressed_streaming");

    [Fact]
    public Task ServerCompressedUnary() => InteropTestCase("server_compressed_unary");

    [Fact]
    public Task ServerCompressedStreaming() => InteropTestCase("server_compressed_streaming");

    private async Task InteropTestCase(string name)
    {
        using (var serverProcess = new WebsiteProcess(_serverPath, _output))
        {
            try
            {
                await serverProcess.WaitForReady().TimeoutAfter(DefaultTimeout);
            }
            catch (Exception ex)
            {
                var errorMessage = $@"Error while running server process.

Server ready: {serverProcess.IsReady}

Server process output:
======================================
{serverProcess.GetOutput()}
======================================";
                throw new InvalidOperationException(errorMessage, ex);
            }

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
