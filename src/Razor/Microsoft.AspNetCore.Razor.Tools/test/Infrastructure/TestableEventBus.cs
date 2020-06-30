// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.Tools
{
    internal class TestableEventBus : EventBus
    {
        public event EventHandler Listening;
        public event EventHandler CompilationComplete;

        public int ListeningCount { get; private set; }

        public int ConnectionCount { get; private set; }

        public int CompletedCount { get; private set; }

        public DateTime? LastProcessedTime { get; private set; }

        public TimeSpan? KeepAlive { get; private set; }

        public bool HasDetectedBadConnection { get; private set; }

        public bool HitKeepAliveTimeout { get; private set; }

        public override void ConnectionListening()
        {
            ListeningCount++;
            Listening?.Invoke(this, EventArgs.Empty);
        }

        public override void ConnectionReceived()
        {
            ConnectionCount++;
        }

        public override void ConnectionCompleted(int count)
        {
            CompletedCount += count;
            LastProcessedTime = DateTime.Now;
        }

        public override void CompilationCompleted()
        {
            CompilationComplete?.Invoke(this, EventArgs.Empty);
        }

        public override void UpdateKeepAlive(TimeSpan timeSpan)
        {
            KeepAlive = timeSpan;
        }

        public override void ConnectionRudelyEnded()
        {
            HasDetectedBadConnection = true;
        }

        public override void KeepAliveReached()
        {
            HitKeepAliveTimeout = true;
        }
    }
}
