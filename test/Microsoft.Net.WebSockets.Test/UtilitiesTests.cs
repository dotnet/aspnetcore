using System;
using System.Text;
using Xunit;

namespace Microsoft.Net.WebSockets.Test
{
    public class UtilitiesTests
    {
        [Fact]
        public void MaskDataRoundTrips()
        {
            byte[] data = Encoding.UTF8.GetBytes("Hello World");
            byte[] orriginal = Encoding.UTF8.GetBytes("Hello World");
            Utilities.MaskInPlace(16843009, new ArraySegment<byte>(data));
            Utilities.MaskInPlace(16843009, new ArraySegment<byte>(data));
            Assert.Equal(orriginal, data);
        }
    }
}
