// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;

namespace Microsoft.VisualStudio.LanguageServices.Razor
{
    public abstract class ForegroundDispatcherTestBase
    {
        internal ForegroundDispatcher Dispatcher { get; } = new SingleThreadedForegroundDispatcher();

        private class SingleThreadedForegroundDispatcher : ForegroundDispatcher
        {
            private Thread Thread { get; } = Thread.CurrentThread;

            public override bool IsForegroundThread => Thread.CurrentThread == Thread;
        }
    }
}
