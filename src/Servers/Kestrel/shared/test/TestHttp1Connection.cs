// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.InternalTesting;

internal class TestHttp1Connection : Http1Connection
{
    public TestHttp1Connection(HttpConnectionContext context)
        : base(context)
    {
    }

    public HttpVersion HttpVersionEnum
    {
        get => _httpVersion;
        set => _httpVersion = value;
    }

    public bool KeepAlive
    {
        get => _keepAlive;
        set => _keepAlive = value;
    }

    public MessageBody NextMessageBody { private get; set; }

    public Task ProduceEndAsync()
    {
        return ProduceEnd();
    }

    /// <summary>
    /// Simulates the beginning of request parsing to test timeout behavior.
    /// This triggers the same timeout logic as the real parsing path without the full parsing overhead.
    /// </summary>
    public void SimulateReadRequestStart()
    {
        if (_requestProcessingStatus == RequestProcessingStatus.RequestPending)
        {
            TimeoutControl.ResetTimeout(ServerOptions.Limits.RequestHeadersTimeout, TimeoutReason.RequestHeaders);
            _requestProcessingStatus = RequestProcessingStatus.ParsingRequestLine;
        }
    }

    protected override MessageBody CreateMessageBody()
    {
        return NextMessageBody ?? base.CreateMessageBody();
    }
}
