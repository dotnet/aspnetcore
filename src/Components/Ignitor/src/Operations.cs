// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;

#nullable enable
namespace Ignitor
{
    public sealed class Operations
    {
        public ConcurrentQueue<CapturedAttachComponentCall> AttachComponent { get; } = new ConcurrentQueue<CapturedAttachComponentCall>();

        public ConcurrentQueue<CapturedRenderBatch> Batches { get; } = new ConcurrentQueue<CapturedRenderBatch>();

        public ConcurrentQueue<string> DotNetCompletions { get; } = new ConcurrentQueue<string>();

        public ConcurrentQueue<string> Errors { get; } = new ConcurrentQueue<string>();

        public ConcurrentQueue<CapturedJSInteropCall> JSInteropCalls { get; } = new ConcurrentQueue<CapturedJSInteropCall>();
    }
}
#nullable restore
