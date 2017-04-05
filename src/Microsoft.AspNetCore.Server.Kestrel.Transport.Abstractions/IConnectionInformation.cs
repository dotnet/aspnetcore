// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO.Pipelines;
using System.Net;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions
{
    public interface IConnectionInformation
    {
        IPEndPoint RemoteEndPoint { get; }
        IPEndPoint LocalEndPoint { get; }

        PipeFactory PipeFactory { get; }
        IScheduler InputWriterScheduler { get; }
        IScheduler OutputReaderScheduler { get; }

        // TODO: Remove timeout management from transport
        ITimeoutControl TimeoutControl { get; }
    }
}
