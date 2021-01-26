// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics.Tracing;

namespace Microsoft.AspNetCore.Internal
{
    internal class TestEventListener : EventListener
    {
        private readonly int _eventId;

        public TestEventListener(int eventId)
        {
            _eventId = eventId;
        }

        public EventWrittenEventArgs EventData { get; private set; }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            // The tests here run in parallel, capture the EventData that a test is explicitly
            // looking for and not give back other tests' data.
            if (eventData.EventId == _eventId)
            {
                EventData = eventData;
            }
        }
    }
}
