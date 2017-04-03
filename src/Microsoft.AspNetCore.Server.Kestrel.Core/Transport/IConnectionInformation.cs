// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Internal.System.IO.Pipelines;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport
{
    public interface IConnectionInformation
    {
        ListenOptions ListenOptions { get; }
        IPEndPoint RemoteEndPoint { get; }
        IPEndPoint LocalEndPoint { get; }

        PipeFactory PipeFactory { get; }
        IScheduler InputWriterScheduler { get; }
        IScheduler OutputWriterScheduler { get; }

        // TODO: Remove timeout management from transport
        ITimeoutControl TimeoutControl { get; }
    }
}
