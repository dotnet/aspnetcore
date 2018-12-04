using System.IO;
using Microsoft.AspNetCore.ResponseCompression;

namespace ResponseCompressionSample
{
    public class CustomCompressionProvider : ICompressionProvider
    {
        public string EncodingName => "custom";

        public bool SupportsFlush => true;

        public Stream CreateStream(Stream outputStream)
        {
            // Create a custom compression stream wrapper here
            return outputStream;
        }
    }
}
