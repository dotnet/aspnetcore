using Microsoft.AspNetCore.Server.Kestrel.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Networking;
using Microsoft.AspNetCore.Server.KestrelTests.TestHelpers;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Server.KestrelTests
{
    public class UvStreamHandleTests
    {
        [Fact]
        public void ReadStopIsIdempotent()
        {
            var mockKestrelTrace = Mock.Of<IKestrelTrace>();
            var mockUvLoopHandle = new Mock<UvLoopHandle>(mockKestrelTrace).Object;
            mockUvLoopHandle.Init(new MockLibuv());

            // Need to mock UvTcpHandle instead of UvStreamHandle, since the latter lacks an Init() method
            var mockUvStreamHandle = new Mock<UvTcpHandle>(mockKestrelTrace).Object;
            mockUvStreamHandle.Init(mockUvLoopHandle, null);

            mockUvStreamHandle.ReadStop();
            mockUvStreamHandle.ReadStop();
        }
    }
}
