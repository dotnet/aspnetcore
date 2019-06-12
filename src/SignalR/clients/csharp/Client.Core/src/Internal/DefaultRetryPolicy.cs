// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.SignalR.Client.Internal
{
    internal class DefaultRetryPolicy : IRetryPolicy
    {
        internal static TimeSpan?[] DEFAULT_RETRY_DELAYS_IN_MILLISECONDS = new TimeSpan?[]
        {
            TimeSpan.Zero,
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(30),
            null,
        };

        private TimeSpan?[] _retryDelays;

        public DefaultRetryPolicy()
        {
            _retryDelays = DEFAULT_RETRY_DELAYS_IN_MILLISECONDS;
        }

        public DefaultRetryPolicy(TimeSpan[] retryDelays)
        {
            _retryDelays = new TimeSpan?[retryDelays.Length + 1];

            for (int i = 0; i < retryDelays.Length; i++)
            {
                _retryDelays[i] = retryDelays[i];
            }
        }

        public TimeSpan? NextRetryDelay(RetryContext retryContext)
        {
            return _retryDelays[retryContext.PreviousRetryCount];
        }
    }
}
