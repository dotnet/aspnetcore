using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Components.Web.Extensions
{
    internal readonly struct Block
    {
        public Task<string> Base64 { get; }

        public int LengthBytes { get; }

        public Block(Task<string> base64, int lengthBytes)
        {
            Base64 = base64;
            LengthBytes = lengthBytes;
        }
    }
}
