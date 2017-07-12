// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using Microsoft.AspNetCore.Server.Kestrel.Internal.System.IO.Pipelines;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal;

namespace Microsoft.AspNetCore.Server.Kestrel.Performance
{
    public class MockConnectionInformation : IConnectionInformation
    {
        public IPEndPoint RemoteEndPoint { get; }
        public IPEndPoint LocalEndPoint { get; }

        public PipeFactory PipeFactory { get; set; }
        public bool RequiresDispatch { get; }
        public IScheduler InputWriterScheduler { get; }
        public IScheduler OutputReaderScheduler { get; }
    }
}
