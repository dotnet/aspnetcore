// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting;

namespace TestStartupAssembly1;

/// <summary>
/// A hosting startup that fails, but only when a test opts in through
/// <see cref="ThrowSettingKey"/>. The opt-in keeps this assembly usable by the tests that
/// expect it to start cleanly, while letting one test assert that a hosting startup which
/// throws does not stop the remaining hosting startups in the same assembly from running.
/// </summary>
/// <remarks>
/// The <c>[assembly: HostingStartup]</c> attribute for this type is declared in
/// TestHostingStartup1.cs, ahead of the one for <see cref="TestHostingStartup1"/>, because
/// the regression this guards depends on the failing startup running first.
/// </remarks>
public class TestThrowingHostingStartup : IHostingStartup
{
    public const string ThrowSettingKey = "throwinhostingstartup";
    public const string ExceptionMessage = "This hosting startup fails on purpose.";

    public void Configure(IWebHostBuilder builder)
    {
        if (string.Equals(builder.GetSetting(ThrowSettingKey), "true", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(ExceptionMessage);
        }
    }
}
