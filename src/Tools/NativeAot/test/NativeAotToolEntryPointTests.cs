// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Tools.Internal;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Tools.NativeAot.Tests;

public class NativeAotToolEntryPointTests
{
    private readonly ITestOutputHelper _output;

    public NativeAotToolEntryPointTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [ConditionalTheory]
    [InlineData("dotnet-dev-certs", "", 0, "Usage: dotnet dev-certs")]
    [InlineData("dotnet-dev-certs", "--help", 0, "Usage: dotnet dev-certs")]
    [InlineData("dotnet-user-jwts", "", 0, "Usage: dotnet user-jwts")]
    [InlineData("dotnet-user-jwts", "--help", 0, "Usage: dotnet user-jwts")]
    [InlineData("dotnet-user-secrets", "", 2, "Usage: dotnet user-secrets")]
    [InlineData("dotnet-user-secrets", "--help", 2, "Usage: dotnet user-secrets")]
    [SkipOnHelix("Native AOT publishing is not supported on these queues.", Queues = HelixConstants.NativeAotNotSupportedHelixQueues)]
    public async Task PublishedNativeAotToolEntryPointRuns(string toolName, string argument, int expectedExitCode, string expectedOutput)
    {
        using var temporaryDirectory = new TemporaryDirectory();
        temporaryDirectory.Create();

        var result = await NativeAotToolRunner.RunAsync(
            toolName,
            string.IsNullOrEmpty(argument) ? [] : [argument],
            _output,
            environmentVariables: NativeAotToolRunner.CreateIsolatedUserProfileEnvironment(temporaryDirectory));

        Assert.Equal(expectedExitCode, result.ExitCode);
        Assert.Contains(expectedOutput, result.AllOutput);
    }
}
