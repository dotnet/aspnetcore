using System;

namespace Microsoft.AspNet.Security.DataProtection {
    /// <summary>
    /// Represents an object that can perform cryptographic operations.
    /// </summary>
    public interface IDataProtector : IDisposable {
        /// <summary>
        /// Given a subpurpose, returns a new IDataProtector that has unique cryptographic keys tied <em>both</em> the purpose
        /// that was used to create this IDataProtector instance <em>and</em> the purpose that is provided as a parameter
        /// to this method.
        /// </summary>
        /// <param name="purpose">The sub-consumer of the IDataProtector.</param>
        /// <returns>An IDataProtector.</returns>
        IDataProtector CreateSubProtector(string purpose);

        /// <summary>
        /// Cryptographically protects some input data.
        /// </summary>
        /// <param name="unprotectedData">The data to be protected.</param>
        /// <returns>An array containing cryptographically protected data.</returns>
        /// <remarks>To retrieve the original data, call Unprotect on the protected data.</remarks>
        byte[] Protect(byte[] unprotectedData);

        /// <summary>
        /// Retrieves the original data that was protected by a call to Protect.
        /// </summary>
        /// <param name="protectedData">The protected data to be decrypted.</param>
        /// <returns>The original data.</returns>
        /// <remarks>Throws CryptographicException if the <em>protectedData</em> parameter has been tampered with.</remarks>
        byte[] Unprotect(byte[] protectedData);
    }
}
