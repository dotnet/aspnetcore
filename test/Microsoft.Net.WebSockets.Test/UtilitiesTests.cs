using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            Utilities.Mask(16843009, new ArraySegment<byte>(data));
            Utilities.Mask(16843009, new ArraySegment<byte>(data));
            Assert.Equal(orriginal, data);
        }
    }
}
