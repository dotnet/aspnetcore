// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.CompilerServices;

namespace Microsoft.VisualStudio.LanguageServices.Razor
{
    internal abstract class ForegroundDispatcher
    {
        public abstract bool IsForegroundThread { get; }

        public virtual void AssertForegroundThread([CallerMemberName] string caller = null)
        {
            if (!IsForegroundThread)
            {
                caller = caller == null ? Resources.ForegroundDispatcher_NoMethodNamePlaceholder : $"'{caller}'";
                throw new InvalidOperationException(Resources.FormatForegroundDispatcher_AssertForegroundThread(caller));
            }
        }

        public virtual void AssertBackgroundThread([CallerMemberName] string caller = null)
        {
            if (IsForegroundThread)
            {
                caller = caller == null ? Resources.ForegroundDispatcher_NoMethodNamePlaceholder : $"'{caller}'";
                throw new InvalidOperationException(Resources.FormatForegroundDispatcher_AssertBackgroundThread(caller));
            }
        }
    }
}
