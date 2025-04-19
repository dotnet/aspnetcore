// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using InteropTests.Helpers;
using Microsoft.AspNetCore.InternalTesting;
using Xunit.Abstractions;

namespace InteropTests;

// All interop test cases, minus GCE authentication specific tests.
// Tests are separate methods so that they can be quarantined separately.
[Retry]
public class InteropTests
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(100);
    private readonly string _clientPath = Path.Combine(Directory.GetCurrentDirectory(), "InteropClient", "InteropClient.dll");
    private readonly string _serverPath = Path.Combine(Directory.GetCurrentDirectory(), "InteropWebsite", "InteropWebsite.dll");
    private readonly ITestOutputHelper _output;

    public InteropTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/61057")]
    public Task EmptyUnary() => InteropTestCase("empty_unary");

    [Fact]
    [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/61057")]
    public Task LargeUnary() => InteropTestCase("large_unary");

    [Fact]
    public Task ClientStreaming() => InteropTestCase("client_streaming");

    [Fact]
    public Task ServerStreaming() => InteropTestCase("server_streaming");

    [Fact]
    public Task PingPong() => InteropTestCase("ping_pong");

    [Fact]
    [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/61051")]
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
    [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/60245")]
    public Task SpecialStatusMessage() => InteropTestCase("special_status_message");

    [Fact]
    public Task UnimplementedService() => InteropTestCase("unimplemented_service");

    [Fact]
    [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/55652")]
    public Task UnimplementedMethod() => InteropTestCase("unimplemented_method");

    [Fact]
    public Task ClientCompressedUnary() => InteropTestCase("client_compressed_unary");

    [Fact]
    public Task ClientCompressedStreaming() => InteropTestCase("client_compressed_streaming");

    [Fact]
    public Task ServerCompressedUnary() => InteropTestCase("server_compressed_unary");

    [Fact]
    [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/60903")]
    public Task ServerCompressedStreaming() => InteropTestCase("server_compressed_streaming");

    private async Task InteropTestCase(string name)
    {
        // Building interop tests processes can be flaky. Sometimes it times out.
        // To mitigate this, we retry the test case a few times on timeout.
        const int maxRetries = 3;
        var attempt = 0;

        while (true)
        {
            attempt++;

            try
            {
                await InteropTestCaseCore(name);
                break; // Exit loop on success
            }
            catch (TimeoutException ex)
            {
                _output.WriteLine($"Attempt {attempt} failed: {ex.Message}");

                if (attempt == maxRetries)
                {
                    _output.WriteLine("Maximum retry attempts reached. Giving up.");
                    throw;
                }
                else
                {
                    await Task.Delay(TimeSpan.FromSeconds(1));
                }
            }
        }
    }

    private async Task InteropTestCaseCore(string name)
    {
        _output.WriteLine($"Starting {nameof(WebsiteProcess)}.");
        using (var serverProcess = new WebsiteProcess(_serverPath, _output))
        {
            try
            {
                _output.WriteLine($"Waiting for {nameof(WebsiteProcess)} to be ready.");
                await serverProcess.WaitForReady().TimeoutAfter(DefaultTimeout);
            }
            catch (Exception ex) when (ex is not TimeoutException)
            {
                var errorMessage = $@"Error while running server process.

Server ready: {serverProcess.IsReady}

Server process output:
======================================
{serverProcess.GetOutput()}
======================================";
                throw new InvalidOperationException(errorMessage, ex);
            }

            _output.WriteLine($"Starting {nameof(ClientProcess)}.");
            using (var clientProcess = new ClientProcess(_output, _clientPath, serverProcess.ServerPort, name))
            {
                try
                {
                    _output.WriteLine($"Waiting for {nameof(ClientProcess)} to be ready.");
                    await clientProcess.WaitForReadyAsync().TimeoutAfter(DefaultTimeout);

                    _output.WriteLine($"Waiting for {nameof(ClientProcess)} to exit.");
                    await clientProcess.WaitForExitAsync().TimeoutAfter(DefaultTimeout);

                    Assert.Equal(0, clientProcess.ExitCode);
                }
                catch (Exception ex) when (ex is not TimeoutException)
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
