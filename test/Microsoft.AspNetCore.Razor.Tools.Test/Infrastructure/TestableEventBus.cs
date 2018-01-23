// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.AspNetCore.Razor.Tools
{
    internal class TestableEventBus : EventBus
    {
        public int ListeningCount;
        public int ConnectionCount;
        public int CompletedCount;
        public DateTime? LastProcessedTime;
        public TimeSpan? KeepAlive;
        public bool HasDetectedBadConnection;
        public bool HitKeepAliveTimeout;
        public event EventHandler Listening;

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
