// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Tests
{
    public class EventRaisingResourceCounter : ResourceCounter
    {
        private readonly ResourceCounter _wrapped;

        public EventRaisingResourceCounter(ResourceCounter wrapped)
        {
            _wrapped = wrapped;
        }

        public event EventHandler OnRelease;
        public event EventHandler<bool> OnLock;

        public override void ReleaseOne()
        {
            _wrapped.ReleaseOne();
            OnRelease?.Invoke(this, EventArgs.Empty);
        }

        public override bool TryLockOne()
        {
            var retVal = _wrapped.TryLockOne();
            OnLock?.Invoke(this, retVal);
            return retVal;
        }
    }
}
