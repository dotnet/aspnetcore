// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
