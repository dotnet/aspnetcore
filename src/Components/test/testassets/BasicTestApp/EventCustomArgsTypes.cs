using System;
using Microsoft.AspNetCore.Components;

namespace BasicTestApp.CustomEventTypesNamespace
{
    [EventHandler("ontestevent", typeof(TestEventArgs), true, true)]
    public static class EventHandlers
    {
    }

    class TestEventArgs : EventArgs
    {
        public string MyProp { get; set; }
    }
}
