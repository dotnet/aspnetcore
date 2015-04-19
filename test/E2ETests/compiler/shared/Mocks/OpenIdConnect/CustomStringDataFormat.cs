#if TESTING
using Microsoft.AspNet.Authentication;

namespace MusicStore.Mocks.OpenIdConnect
{
    internal class CustomStringDataFormat : ISecureDataFormat<string>
    {
        private const string _capturedNonce = "635579928639517715.OTRjOTVkM2EtMDRmYS00ZDE3LThhZGUtZWZmZGM4ODkzZGZkMDRlNDhkN2MtOWIwMC00ZmVkLWI5MTItMTUwYmQ4MzdmOWI0";
        public string Protect(string data)
        {
            return "protectedString";
        }

        public string Unprotect(string protectedText)
        {
            return protectedText == "protectedString" ? _capturedNonce : null;
        }
    }
}
#endif