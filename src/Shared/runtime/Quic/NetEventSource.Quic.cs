// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics.Tracing;

namespace System.Net
{
    [EventSource(Name = "Microsoft-System-Net-Quic")]
    internal sealed partial class NetEventSource : EventSource
    {
    }
}
