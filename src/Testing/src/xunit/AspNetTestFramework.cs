// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.InternalTesting;

public class AspNetTestFramework : XunitTestFramework
{
    public AspNetTestFramework(IMessageSink messageSink)
        : base(messageSink)
    {
    }

    protected override ITestFrameworkExecutor CreateExecutor(AssemblyName assemblyName)
        => new AspNetTestFrameworkExecutor(assemblyName, SourceInformationProvider, DiagnosticMessageSink);
}
