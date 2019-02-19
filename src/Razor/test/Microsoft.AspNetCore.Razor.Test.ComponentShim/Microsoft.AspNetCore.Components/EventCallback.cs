// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Components
{
    public readonly struct EventCallback
    {
        public static readonly EventCallbackFactory Factory = new EventCallbackFactory();

        internal readonly MulticastDelegate Delegate;
        internal readonly IHandleEvent Receiver;

        public EventCallback(IHandleEvent receiver, MulticastDelegate @delegate)
        {
            Receiver = receiver;
            Delegate = @delegate;
        }
    }

    public readonly struct EventCallback<T>
    {
        internal readonly MulticastDelegate Delegate;
        internal readonly IHandleEvent Receiver;

        public EventCallback(IHandleEvent receiver, MulticastDelegate @delegate)
        {
            Receiver = receiver;
            Delegate = @delegate;
        }
    }
}
