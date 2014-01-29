using System;

#if !NET45
namespace System.Security.Cryptography {
    internal sealed class CryptographicException : Exception {
        internal CryptographicException(string message)
            : base(message) {

        }

        internal CryptographicException(int unused) {
        }
    }
}
#endif