// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests;

public class StrictTestServerTests : LoggedTest
{
    public override void Dispose()
    {
        base.Dispose();

        if (TestSink.Writes.FirstOrDefault(w => w.LogLevel > LogLevel.Information) is WriteContext writeContext)
        {
            throw new XunitException($"Unexpected log: {writeContext}");
        }
    }

    protected static TaskCompletionSource<bool> CreateTaskCompletionSource()
    {
        return new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}
