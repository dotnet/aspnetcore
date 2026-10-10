// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.Hosting;

// Both attributes live in this file so that their relative order is fixed: assembly-level
// attributes are emitted in source order within a file, but the order across files depends on
// the order the compiler happens to receive them. TestThrowingHostingStartup is deliberately
// first so tests can cover a failing hosting startup running before a later one.
[assembly: HostingStartup(typeof(TestStartupAssembly1.TestThrowingHostingStartup))]
[assembly: HostingStartup(typeof(TestStartupAssembly1.TestHostingStartup1))]

namespace TestStartupAssembly1;

public class TestHostingStartup1 : IHostingStartup
{
    public void Configure(IWebHostBuilder builder)
    {
        var calls = builder.GetSetting("testhostingstartup1_calls");
        var numCalls = 1;

        if (calls != null)
        {
            numCalls = int.Parse(calls, CultureInfo.InvariantCulture) + 1;
        }

        builder.UseSetting("testhostingstartup1", "1");
        builder.UseSetting("testhostingstartup_chain", builder.GetSetting("testhostingstartup_chain") + "1");
        builder.UseSetting("testhostingstartup1_calls", numCalls.ToString(CultureInfo.InvariantCulture));
    }
}
