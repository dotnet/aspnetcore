// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.Expiration.Interfaces;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class TestFileTrigger : IExpirationTrigger
    {
        public bool ActiveExpirationCallbacks { get; } = false;

        public bool IsExpired { get; set; }

        public IDisposable RegisterExpirationCallback(Action<object> callback, object state)
        {
            throw new NotImplementedException();
        }
    }
}