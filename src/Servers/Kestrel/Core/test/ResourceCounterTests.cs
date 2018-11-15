// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public class ResourceCounterTests
    {
        [Theory]
        [InlineData(-1)]
        [InlineData(long.MinValue)]
        public void QuotaInvalid(long max)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ResourceCounter.Quota(max));
        }

        [Fact]
        public void QuotaAcceptsUpToButNotMoreThanMax()
        {
            var counter = ResourceCounter.Quota(1);
            Assert.True(counter.TryLockOne());
            Assert.False(counter.TryLockOne());
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        public void QuotaValid(long max)
        {
            var counter = ResourceCounter.Quota(max);
            Parallel.For(0, max, i =>
            {
                Assert.True(counter.TryLockOne());
            });

            Parallel.For(0, 10, i =>
            {
                Assert.False(counter.TryLockOne());
            });
        }

        [Fact]
        public void QuotaDoesNotWrapAround()
        {
            var counter = new ResourceCounter.FiniteCounter(long.MaxValue);
            counter.Count = long.MaxValue;
            Assert.False(counter.TryLockOne());
        }
    }
}
