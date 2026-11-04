// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.Hosting;

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
