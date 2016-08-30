// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Infrastructure;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.KestrelTests
{
    public class DateHeaderValueManagerTests
    {
        [Fact]
        public void GetDateHeaderValue_ReturnsDateValueInRFC1123Format()
        {
            var now = DateTimeOffset.UtcNow;
            var systemClock = new MockSystemClock
            {
                UtcNow = now
            };
            var timeWithoutRequestsUntilIdle = TimeSpan.FromSeconds(1);
            var timerInterval = TimeSpan.FromSeconds(10);
            var dateHeaderValueManager = new DateHeaderValueManager(systemClock, timeWithoutRequestsUntilIdle, timerInterval);
            string result;

            try
            {
                result = dateHeaderValueManager.GetDateHeaderValues().String;
            }
            finally
            {
                dateHeaderValueManager.Dispose();
            }

            Assert.Equal(now.ToString(Constants.RFC1123DateFormat), result);
        }

        [Fact]
        public void GetDateHeaderValue_ReturnsCachedValueBetweenTimerTicks()
        {
            var now = DateTimeOffset.UtcNow;
            var future = now.AddSeconds(10);
            var systemClock = new MockSystemClock
            {
                UtcNow = now
            };
            var timeWithoutRequestsUntilIdle = TimeSpan.FromSeconds(1);
            var timerInterval = TimeSpan.FromSeconds(10);
            var dateHeaderValueManager = new DateHeaderValueManager(systemClock, timeWithoutRequestsUntilIdle, timerInterval);
            string result1;
            string result2;

            try
            {
                result1 = dateHeaderValueManager.GetDateHeaderValues().String;
                systemClock.UtcNow = future;
                result2 = dateHeaderValueManager.GetDateHeaderValues().String;
            }
            finally
            {
                dateHeaderValueManager.Dispose();
            }

            Assert.Equal(now.ToString(Constants.RFC1123DateFormat), result1);
            Assert.Equal(now.ToString(Constants.RFC1123DateFormat), result2);
            Assert.Equal(1, systemClock.UtcNowCalled);
        }

        [Fact]
        public async Task GetDateHeaderValue_ReturnsUpdatedValueAfterIdle()
        {
            var now = DateTimeOffset.UtcNow;
            var future = now.AddSeconds(10);
            var systemClock = new MockSystemClock
            {
                UtcNow = now
            };
            var timeWithoutRequestsUntilIdle = TimeSpan.FromMilliseconds(250);
            var timerInterval = TimeSpan.FromMilliseconds(100);
            var dateHeaderValueManager = new DateHeaderValueManager(systemClock, timeWithoutRequestsUntilIdle, timerInterval);
            string result1;
            string result2;

            try
            {
                result1 = dateHeaderValueManager.GetDateHeaderValues().String;
                systemClock.UtcNow = future;
                // Wait for longer than the idle timeout to ensure the timer is stopped
                await Task.Delay(TimeSpan.FromSeconds(1));
                result2 = dateHeaderValueManager.GetDateHeaderValues().String;
            }
            finally
            {
                dateHeaderValueManager.Dispose();
            }

            Assert.Equal(now.ToString(Constants.RFC1123DateFormat), result1);
            Assert.Equal(future.ToString(Constants.RFC1123DateFormat), result2);
            Assert.True(systemClock.UtcNowCalled >= 2);
        }

        [Fact]
        public void GetDateHeaderValue_ReturnsDateValueAfterDisposed()
        {
            var now = DateTimeOffset.UtcNow;
            var future = now.AddSeconds(10);
            var systemClock = new MockSystemClock
            {
                UtcNow = now
            };
            var timeWithoutRequestsUntilIdle = TimeSpan.FromSeconds(1);
            var timerInterval = TimeSpan.FromSeconds(10);
            var dateHeaderValueManager = new DateHeaderValueManager(systemClock, timeWithoutRequestsUntilIdle, timerInterval);

            var result1 = dateHeaderValueManager.GetDateHeaderValues().String;
            dateHeaderValueManager.Dispose();
            systemClock.UtcNow = future;
            var result2 = dateHeaderValueManager.GetDateHeaderValues().String;
            
            Assert.Equal(now.ToString(Constants.RFC1123DateFormat), result1);
            Assert.Equal(future.ToString(Constants.RFC1123DateFormat), result2);
        }
    }
}
