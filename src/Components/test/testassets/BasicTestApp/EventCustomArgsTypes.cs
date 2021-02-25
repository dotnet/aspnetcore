using System;
using Microsoft.AspNetCore.Components;

namespace BasicTestApp.CustomEventTypesNamespace
{
    [EventHandler("ontestevent", typeof(TestEventArgs), true, true)]
    [EventHandler("onkeydown.testvariant", typeof(TestKeyDownEventArgs), true, true)]
    [EventHandler("onkeydown.yetanother", typeof(YetAnotherCustomKeyboardEventArgs), true, true)]
    [EventHandler("oncustommouseover", typeof(EventArgs), true, true)]
    public static class EventHandlers
    {
    }

    class TestEventArgs : EventArgs
    {
        public string MyProp { get; set; }
    }

    class TestKeyDownEventArgs : EventArgs
    {
        public string CustomKeyInfo { get; set; }
    }

    class YetAnotherCustomKeyboardEventArgs : EventArgs
    {
        public string YouPressed { get; set; }
    }
}
