// Copyright (c) .NET Foundation. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Http.Features;
using Microsoft.AspNet.Http.Internal;
using Xunit;

namespace Microsoft.AspNet.Http.Extensions.Tests
{
    public class SendFileResponseExtensionsTests
    {
        [Fact]
        public void SendFileSupport()
        {
            var context = new DefaultHttpContext();
            var response = context.Response;
            Assert.False(response.SupportsSendFile());
            context.Features.Set<IHttpSendFileFeature>(new FakeSendFileFeature());
            Assert.True(response.SupportsSendFile());
        }

        [Fact]
        public Task SendFileWhenNotSupported()
        {
            var response = new DefaultHttpContext().Response;
            return Assert.ThrowsAsync<NotSupportedException>(() => response.SendFileAsync("foo"));
        }

        [Fact]
        public async Task SendFileWorks()
        {
            var context = new DefaultHttpContext();
            var response = context.Response;
            var fakeFeature = new FakeSendFileFeature();
            context.Features.Set<IHttpSendFileFeature>(fakeFeature);

            await response.SendFileAsync("bob", 1, 3, CancellationToken.None);

            Assert.Equal("bob", fakeFeature.name);
            Assert.Equal(1, fakeFeature.offset);
            Assert.Equal(3, fakeFeature.length);
            Assert.Equal(CancellationToken.None, fakeFeature.token);
        }

        private class FakeSendFileFeature : IHttpSendFileFeature
        {
            public string name = null;
            public long offset = 0;
            public long? length = null;
            public CancellationToken token;

            public Task SendFileAsync(string path, long offset, long? length, CancellationToken cancellation)
            {
                this.name = path;
                this.offset = offset;
                this.length = length;
                this.token = cancellation;
                return Task.FromResult(0);
            }
        }
    }
}
