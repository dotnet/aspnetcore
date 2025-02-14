// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.InternalTesting;
using Xunit;

namespace AlwaysTestTests;

/// <summary>
/// Tests for every test assembly to ensure quarantined and unquarantined runs report at least one test execution.
/// </summary>
public class SuccessfulTests
{
    /// <summary>
    /// Test that executes in quarantined runs and always succeeds.
    /// </summary>
    [Fact]
    [QuarantinedTest("No issue")]
    public void GuaranteedQuarantinedTest()
    {
    }

    /// <summary>
    /// Test that executes in unquarantined runs and always succeeds.
    /// </summary>
    /// <remarks>
    /// <see cref="TraitAttribute"/> applied to ensure test runs even if assembly is quarantined overall.
    /// <c>dotnet test --filter</c>, <c>dotnet vstest --testcasefilter</c>, and
    /// <c>vstest.console.exe --testcasefilter</c> handle the "no Quarantined=true trait OR Quarantined=false" case
    /// e.g. <c>"Quarantined!=true|Quarantined=false</c>. But, <c>xunit.console.exe</c> doesn't have a syntax to
    /// make it work (yet?).
    /// </remarks>
    [Fact]
    [Trait("Quarantined", "false")]
    public void GuaranteedUnquarantinedTest()
    {
    }
}
