// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.SignalR.Common.Tests.Internal.Protocol;
using Microsoft.AspNetCore.SignalR.Protocol;

namespace Microsoft.AspNetCore.Components.Server.BlazorPack;

public class BlazorPackHubProtocolTest : MessagePackHubProtocolTestBase
{
    protected override IHubProtocol HubProtocol { get; } = new BlazorPackHubProtocol();

    [Fact]
    public void CanRoundTripStringArrayResult()
    {
        var testData = new ProtocolTestData(
            name: "CompletionWithNoHeadersAndStringArrayResult",
            message: CompletionMessage.WithResult("xyz", payload: new[] { "first.value", "second.value" }),
            binary: "lQOAo3h5egOSq2ZpcnN0LnZhbHVlrHNlY29uZC52YWx1ZQ==");

        TestWriteMessages(testData);
        TestParseMessages(testData);
    }
}
