using System.Text;

namespace Microsoft.AspNet.Mvc.Internal
{
    public static class UTF8EncodingWithoutBOM
    {
        public static readonly Encoding Encoding
            = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    }
}