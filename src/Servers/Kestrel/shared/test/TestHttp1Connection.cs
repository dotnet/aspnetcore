// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;

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

    protected override MessageBody CreateMessageBody()
    {
        return NextMessageBody ?? base.CreateMessageBody();
    }
}
