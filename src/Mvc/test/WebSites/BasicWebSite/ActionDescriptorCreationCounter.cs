// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.AspNetCore.Mvc.Abstractions;

namespace BasicWebSite
{
    public class ActionDescriptorCreationCounter : IActionDescriptorProvider
    {
        private long _callCount;

        public long CallCount
        {
            get
            {
                var callCount = Interlocked.Read(ref _callCount);

                return callCount;
            }
        }

        public int Order
        {
            get
            {
                return -1000 - 100;
            }
        }

        public void OnProvidersExecuting(ActionDescriptorProviderContext context)
        {
        }

        public void OnProvidersExecuted(ActionDescriptorProviderContext context)
        {
            if (context.Results.Count == 0)
            {
                throw new InvalidOperationException("No actions found!");
            }

            Interlocked.Increment(ref _callCount);
        }
    }
}