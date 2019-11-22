// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public class StreamsTests
    {
        [Fact]
        public async Task StreamsThrowAfterAbort()
        {
            var streams = new Streams(Mock.Of<IHttpBodyControlFeature>(), Mock.Of<IHttpResponseControl>());
            var (request, response) = streams.Start(new MockMessageBody());

            var ex = new Exception("My error");
            streams.Abort(ex);

            await response.WriteAsync(new byte[1], 0, 1);
            Assert.Same(ex,
                await Assert.ThrowsAsync<Exception>(() => request.ReadAsync(new byte[1], 0, 1)));
        }

        [Fact]
        public async Task StreamsThrowOnAbortAfterUpgrade()
        {
            var streams = new Streams(Mock.Of<IHttpBodyControlFeature>(), Mock.Of<IHttpResponseControl>());
            var (request, response) = streams.Start(new MockMessageBody(upgradeable: true));

            var upgrade = streams.Upgrade();
            var ex = new Exception("My error");
            streams.Abort(ex);

            var writeEx = await Assert.ThrowsAsync<InvalidOperationException>(() => response.WriteAsync(new byte[1], 0, 1));
            Assert.Equal(CoreStrings.ResponseStreamWasUpgraded, writeEx.Message);

            Assert.Same(ex,
                await Assert.ThrowsAsync<Exception>(() => request.ReadAsync(new byte[1], 0, 1)));

            Assert.Same(ex,
                await Assert.ThrowsAsync<Exception>(() => upgrade.ReadAsync(new byte[1], 0, 1)));

            await upgrade.WriteAsync(new byte[1], 0, 1);
        }

        [Fact]
        public async Task StreamsThrowOnUpgradeAfterAbort()
        {
            var streams = new Streams(Mock.Of<IHttpBodyControlFeature>(), Mock.Of<IHttpResponseControl>());

            var (request, response) = streams.Start(new MockMessageBody(upgradeable: true));
            var ex = new Exception("My error");
            streams.Abort(ex);

            var upgrade = streams.Upgrade();

            var writeEx = await Assert.ThrowsAsync<InvalidOperationException>(() => response.WriteAsync(new byte[1], 0, 1));
            Assert.Equal(CoreStrings.ResponseStreamWasUpgraded, writeEx.Message);

            Assert.Same(ex,
                await Assert.ThrowsAsync<Exception>(() => request.ReadAsync(new byte[1], 0, 1)));

            Assert.Same(ex,
                await Assert.ThrowsAsync<Exception>(() => upgrade.ReadAsync(new byte[1], 0, 1)));

            await upgrade.WriteAsync(new byte[1], 0, 1);
        }

        private class MockMessageBody : MessageBody
        {
            public MockMessageBody(bool upgradeable = false)
                : base(null, null)
            {
                RequestUpgrade = upgradeable;
            }
        }
    }
}
