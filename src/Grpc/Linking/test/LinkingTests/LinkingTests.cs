// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Grpc.Tests.Shared;
using Microsoft.AspNetCore.Testing;
using Xunit.Abstractions;

namespace LinkingTests;

// All interop test cases, minus GCE authentication specific tests.
// Tests are separate methods so that they can be quarantined separately.
public class LinkingTests
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);
    private readonly ITestOutputHelper _output;

    public LinkingTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task BasicGrpcApp()
    {
        var projectDirectory = typeof(LinkingTests).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(a => a.Key == "ProjectDirectory")
            .Value;

        var tempPath = Path.GetTempPath();
        var clientPath = Path.Combine(tempPath, "BasicClient");
        var websitePath = Path.Combine(tempPath, "BasicWebsite");

        EnsureDeleted(clientPath);
        EnsureDeleted(websitePath);

        try
        {
            await AppPublisher.PublishAppAsync(_output, projectDirectory, projectDirectory + @"\..\testassets\BasicClient\BasicClient.csproj", clientPath, enableTrimming: true).DefaultTimeout(DefaultTimeout * 2);
            await AppPublisher.PublishAppAsync(_output, projectDirectory, projectDirectory + @"\..\testassets\BasicWebsite\BasicWebsite.csproj", websitePath, enableTrimming: true).DefaultTimeout(DefaultTimeout * 2);

            await RunApps(clientPath, websitePath);
        }
        finally
        {
            EnsureDeleted(clientPath);
            EnsureDeleted(websitePath);
        }
    }

    private static void EnsureDeleted(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }

    private async Task RunApps(string clientPath, string serverPath)
    {
        using (var serverProcess = new WebsiteProcess(Path.Combine(serverPath, "BasicWebsite.exe"), arguments: string.Empty, _output))
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

            var arguments = @$"http://localhost:{serverProcess.ServerPort}";
            using (var clientProcess = new ClientProcess(_output, Path.Combine(clientPath, "BasicClient.exe"), arguments))
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
