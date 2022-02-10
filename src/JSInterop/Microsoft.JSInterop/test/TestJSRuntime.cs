// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.JSInterop.Infrastructure;

namespace Microsoft.JSInterop;

internal class TestJSRuntime : JSRuntime
{
    protected override void BeginInvokeJS(long asyncHandle, string identifier, string? argsJson, JSCallResultType resultType, long targetInstanceId)
    {
        throw new NotImplementedException();
    }

    protected internal override void EndInvokeDotNet(DotNetInvocationInfo invocationInfo, in DotNetInvocationResult invocationResult)
    {
        throw new NotImplementedException();
    }

    protected internal override void SendByteArray(int id, byte[] data)
    {
        // No-op
    }

    protected internal override Task TransmitStreamAsync(long streamId, DotNetStreamReference dotNetStreamReference)
    {
        // No-op
        return Task.CompletedTask;
    }
}
