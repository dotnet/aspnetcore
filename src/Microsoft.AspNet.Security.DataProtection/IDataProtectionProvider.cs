using System;

namespace Microsoft.AspNet.Security.DataProtection {
    public interface IDataProtectionProvider : IDisposable {
        /// <summary>
        /// Given a purpose, returns a new IDataProtector that has unique cryptographic keys tied to this purpose.
        /// </summary>
        /// <param name="purpose">The consumer of the IDataProtector.</param>
        /// <returns>An IDataProtector.</returns>
        IDataProtector CreateProtector(string purpose);
    }
}
