// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure
{
    internal abstract class ResourceCounter
    {
        public abstract bool TryLockOne();
        public abstract void ReleaseOne();

        public static ResourceCounter Unlimited { get; } = new UnlimitedCounter();
        public static ResourceCounter Quota(long amount) => new FiniteCounter(amount);

        private class UnlimitedCounter : ResourceCounter
        {
            public override bool TryLockOne() => true;
            public override void ReleaseOne()
            {
            }
        }

        internal class FiniteCounter : ResourceCounter
        {
            private readonly long _max;
            private long _count;

            public FiniteCounter(long max)
            {
                if (max < 0)
                {
                    throw new ArgumentOutOfRangeException(CoreStrings.NonNegativeNumberRequired);
                }

                _max = max;
            }

            public override bool TryLockOne()
            {
                var count = _count;

                // Exit if count == MaxValue as incrementing would overflow.

                while (count < _max && count != long.MaxValue)
                {
                    var prev = Interlocked.CompareExchange(ref _count, count + 1, count);
                    if (prev == count)
                    {
                        return true;
                    }

                    // Another thread changed the count before us. Try again with the new counter value.
                    count = prev;
                }

                return false;
            }

            public override void ReleaseOne()
            {
                Interlocked.Decrement(ref _count);

                Debug.Assert(_count >= 0, "Resource count is negative. More resources were released than were locked.");
            }

            // for testing
            internal long Count
            {
                get => _count;
                set => _count = value;
            }
        }
    }
}
