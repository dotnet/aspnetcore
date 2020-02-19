// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.Tracing;

namespace System.Net
{
    [EventSource(Name = "Microsoft-System-Net-Quic")]
    internal sealed partial class NetEventSource : EventSource
    {
    }
}
