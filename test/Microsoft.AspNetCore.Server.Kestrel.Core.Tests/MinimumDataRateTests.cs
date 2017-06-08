// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public class MinimumDataRateTests
    {
        [Theory]
        [InlineData(double.Epsilon)]
        [InlineData(double.MaxValue)]
        public void RateValid(double value)
        {
            Assert.Equal(value, new MinimumDataRate(rate: value, gracePeriod: TimeSpan.Zero).Rate);
        }

        [Theory]
        [InlineData(double.MinValue)]
        [InlineData(0)]
        public void RateInvalid(double value)
        {
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => new MinimumDataRate(rate: value, gracePeriod: TimeSpan.Zero));

            Assert.Equal("rate", exception.ParamName);
            Assert.StartsWith(CoreStrings.PositiveNumberRequired, exception.Message);
        }

        [Theory]
        [MemberData(nameof(GracePeriodValidData))]
        public void GracePeriodValid(TimeSpan value)
        {
            Assert.Equal(value, new MinimumDataRate(rate: 1, gracePeriod: value).GracePeriod);
        }

        [Theory]
        [MemberData(nameof(GracePeriodInvalidData))]
        public void GracePeriodInvalid(TimeSpan value)
        {
            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => new MinimumDataRate(rate: 1, gracePeriod: value));

            Assert.Equal("gracePeriod", exception.ParamName);
            Assert.StartsWith(CoreStrings.NonNegativeTimeSpanRequired, exception.Message);
        }

        public static TheoryData<TimeSpan> GracePeriodValidData => new TheoryData<TimeSpan>
        {
            TimeSpan.Zero,
            TimeSpan.FromTicks(1),
            TimeSpan.MaxValue
        };

        public static TheoryData<TimeSpan> GracePeriodInvalidData => new TheoryData<TimeSpan>
        {
            TimeSpan.MinValue,
            TimeSpan.FromTicks(-1)
        };
    }
}
