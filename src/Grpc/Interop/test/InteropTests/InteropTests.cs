// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Grpc.Tests.Shared;
using Microsoft.AspNetCore.Testing;
using Xunit.Abstractions;

namespace InteropTests;

public class InteropTestsFixture : IDisposable
{
    public string ServerPath { get; private set; }
    public string ClientPath { get; private set; }

    public InteropTestsFixture()
    {
        var tempPath = Path.GetTempPath();
        ServerPath = Path.Combine(tempPath, "InteropWebsite");
        ClientPath = Path.Combine(tempPath, "InteropClient");

        EnsureDeleted(ServerPath);
        EnsureDeleted(ClientPath);
    }

    public void Dispose()
    {
        // Delete deployed files once tests are complete.
        // Retry with delay to avoid file in use errors.
        for (var i = 0; i < 5; i++)
        {
            try
            {
                EnsureDeleted(ServerPath);
                EnsureDeleted(ClientPath);
                break;
            }
            catch
            {
                Thread.Sleep(100);
            }
        }
    }

    private static void EnsureDeleted(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }
}

// All interop test cases, minus GCE authentication specific tests.
// Tests are separate methods so that they can be quarantined separately.
public class InteropTests : IClassFixture<InteropTestsFixture>
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);
    private readonly InteropTestsFixture _fixture;
    private readonly ITestOutputHelper _output;

    public InteropTests(InteropTestsFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
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

    private async Task EnsurePublishedAsync()
    {
        if (!Directory.Exists(_fixture.ServerPath) || !Directory.Exists(_fixture.ClientPath))
        {
            var projectDirectory = typeof(InteropTests).Assembly
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .Single(a => a.Key == "ProjectDirectory")
                .Value;

            var interopWebsiteProject = projectDirectory + @"\..\testassets\InteropWebsite\InteropWebsite.csproj";
            var interopClientProject = projectDirectory + @"\..\testassets\InteropClient\InteropClient.csproj";

            await AppPublisher.PublishAppAsync(_output, projectDirectory, interopWebsiteProject, _fixture.ServerPath);
            await AppPublisher.PublishAppAsync(_output, projectDirectory, interopClientProject, _fixture.ClientPath);
        }
    }

    private async Task InteropTestCase(string name)
    {
        await EnsurePublishedAsync();

        using (var serverProcess = new WebsiteProcess(Path.Combine(_fixture.ServerPath, "InteropWebsite.exe"), arguments: string.Empty, _output))
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

            var arguments = @$"--use_tls false --server_port {serverProcess.ServerPort} --client_type httpclient --test_case {name}";
            using (var clientProcess = new ClientProcess(_output, Path.Combine(_fixture.ClientPath, "InteropClient.exe"), arguments))
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
