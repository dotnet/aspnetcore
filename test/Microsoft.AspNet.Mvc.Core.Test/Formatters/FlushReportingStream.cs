using System.IO;
using System.Threading;
using Moq;

namespace Microsoft.AspNet.Mvc.Core
{
    public static class FlushReportingStream
    {
        public static Stream GetThrowingStream()
        {
            var mock = new Mock<Stream>();
            mock.Verify(m => m.Flush(), Times.Never());
            mock.Verify(m => m.FlushAsync(It.IsAny<CancellationToken>()), Times.Never());

            return mock.Object;
        }
    }
}