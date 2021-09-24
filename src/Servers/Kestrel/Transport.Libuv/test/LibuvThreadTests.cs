// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Tests.TestHelpers;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Tests
{
    public class LibuvThreadTests
    {
        [Fact]
        public async Task LibuvThreadDoesNotThrowIfPostingWorkAfterDispose()
        {
            var mockLibuv = new MockLibuv();
            var transportContext = new TestLibuvTransportContext();
            var thread = new LibuvThread(mockLibuv, transportContext);
            var ranOne = false;
            var ranTwo = false;
            var ranThree = false;
            var ranFour = false;

            await thread.StartAsync();

            await thread.PostAsync<object>(_ =>
            {
                ranOne = true;
            },
            null);

            Assert.Equal(1, mockLibuv.PostCount);

            // Shutdown the libuv thread
            await thread.StopAsync(TimeSpan.FromSeconds(5));

            Assert.Equal(2, mockLibuv.PostCount);

            var task = thread.PostAsync<object>(_ =>
            {
                ranTwo = true;
            },
            null);

            Assert.Equal(2, mockLibuv.PostCount);

            thread.Post<object>(_ =>
            {
                ranThree = true;
            },
            null);

            Assert.Equal(2, mockLibuv.PostCount);

            thread.Schedule(_ =>
            {
                ranFour = true;
            },
            (object)null);

            Assert.Equal(2, mockLibuv.PostCount);

            Assert.True(task.IsCompleted);
            Assert.True(ranOne);
            Assert.False(ranTwo);
            Assert.False(ranThree);
            Assert.False(ranFour);
        }
    }
}
