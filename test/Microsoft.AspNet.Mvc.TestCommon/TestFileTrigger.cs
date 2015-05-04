// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.Caching;

namespace Microsoft.AspNet.Mvc.Razor
{
    internal class TestFileTrigger : IExpirationTrigger
    {
        public bool ActiveExpirationCallbacks { get; } = false;

        public bool IsExpired { get; set; }

        public IDisposable RegisterExpirationCallback(Action<object> callback, object state)
        {
            throw new NotImplementedException();
        }
    }
}