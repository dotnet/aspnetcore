using System;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public class Http3LimitsTests
    {
        [Fact]
        public void CannotUpdateDynamicTableSettings()
        {
            var limits = new Http3Limits();
            Assert.Throws<NotImplementedException>(() => limits.BlockedStreams = 1);
            Assert.Throws<NotImplementedException>(() => limits.HeaderTableSize = 1);
        }
    }
}
