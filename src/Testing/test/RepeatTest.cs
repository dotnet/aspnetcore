// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Testing
{
    [Repeat]
    public class RepeatTest
    {
        public static int _runCount = 0;

        [Fact]
        [Repeat(5)]
        public void RepeatLimitIsSetCorrectly()
        {
            Assert.Equal(5, RepeatContext.Current.Limit);
        }

        [Fact]
        [Repeat(5)]
        public void RepeatRunsTestSpecifiedNumberOfTimes()
        {
            Assert.Equal(RepeatContext.Current.CurrentIteration, _runCount);
            _runCount++;
        }

        [Fact]
        public void RepeatCanBeSetOnClass()
        {
            Assert.Equal(10, RepeatContext.Current.Limit);
        }
    }

    public class LoggedTestXunitRepeatAssemblyTests
    {
        [Fact]
        public void RepeatCanBeSetOnAssembly()
        {
            Assert.Equal(1, RepeatContext.Current.Limit);
        }
    }
}
