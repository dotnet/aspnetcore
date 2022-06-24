// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;

public class WebTransportStreamTests : Http3TestBase
{
    [Fact]
    public async Task CreateRequestStream_RequestCompleted_Disposed()
    {
        var appCompletedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await Http3Api.InitializeConnectionAsync(_noopApplication);

        await Http3Api.CreateControlStream();
        await Http3Api.GetInboundControlStream();

        var requestStream = await Http3Api.CreateRequestStream();

        // test sending a connect request and then checking that the webtransport session was created and initialized
        // test that an aborted or closed stream can't be used
        // test that each stream type sets the CanRead and CanWrite properties correctly
        // come up with a couple more
    }
}
