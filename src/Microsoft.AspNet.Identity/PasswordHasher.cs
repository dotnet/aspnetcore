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
#if NET45
            return Crypto.HashPassword(password);
#else
            return password;
#endif
        }

        /// <summary>
        ///     Verify that a password matches the hashedPassword
        /// </summary>
        /// <param name="hashedPassword"></param>
        /// <param name="providedPassword"></param>
        /// <returns></returns>
        public virtual PasswordVerificationResult VerifyHashedPassword(string hashedPassword, string providedPassword)
        {
#if NET45
            return Crypto.VerifyHashedPassword(hashedPassword, providedPassword) ? PasswordVerificationResult.Success : PasswordVerificationResult.Failed;
#else
            return hashedPassword == providedPassword  ? PasswordVerificationResult.Success : PasswordVerificationResult.Failed;
#endif
        }
    }
}