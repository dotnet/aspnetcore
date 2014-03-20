namespace Microsoft.AspNet.Identity
{
    /// <summary>
    ///     Implements password hashing methods
    /// </summary>
    public class PasswordHasher : IPasswordHasher
    {
        /// <summary>
        ///     Hash a password
        /// </summary>
        /// <param name="password"></param>
        /// <returns></returns>
        public virtual string HashPassword(string password)
        {
            return Crypto.HashPassword(password);
        }

        /// <summary>
        ///     Verify that a password matches the hashedPassword
        /// </summary>
        /// <param name="hashedPassword"></param>
        /// <param name="providedPassword"></param>
        /// <returns></returns>
        public virtual PasswordVerificationResult VerifyHashedPassword(string hashedPassword, string providedPassword)
        {
            return Crypto.VerifyHashedPassword(hashedPassword, providedPassword)
                ? PasswordVerificationResult.Success
                : PasswordVerificationResult.Failed;
        }
    }
}