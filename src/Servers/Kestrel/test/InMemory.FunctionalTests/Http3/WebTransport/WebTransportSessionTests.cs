// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;

public class WebTransportSessionTests : Http3TestBase
{
    [Fact]
    public async Task CreateRequestStream_RequestCompleted_Disposed()
    {
        var appCompletedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await Http3Api.InitializeConnectionAsync(_noopApplication);

        await Http3Api.CreateControlStream();
        await Http3Api.GetInboundControlStream();

        //var requestStream = await Http3Api.CreateRequestStream();

        // make sessions, streams and things and test
    }
}
