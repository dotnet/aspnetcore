// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Buffers;
using System.IO.Pipelines;
using System.Threading;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal
{
    public interface ITransportSchedulerFeature
    {
        PipeScheduler InputWriterScheduler { get; }

        PipeScheduler OutputReaderScheduler { get; }
    }
}
