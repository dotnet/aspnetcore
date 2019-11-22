// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Concurrent;

namespace Ignitor
{
    public sealed class Operations
    {
        public ConcurrentQueue<CapturedRenderBatch> Batches { get; } = new ConcurrentQueue<CapturedRenderBatch>();

        public ConcurrentQueue<string> DotNetCompletions { get; } = new ConcurrentQueue<string>();

        public ConcurrentQueue<string> Errors { get; } = new ConcurrentQueue<string>();

        public ConcurrentQueue<CapturedJSInteropCall> JSInteropCalls { get; } = new ConcurrentQueue<CapturedJSInteropCall>();
    }
}
