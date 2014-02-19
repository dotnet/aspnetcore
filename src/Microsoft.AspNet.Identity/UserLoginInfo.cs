namespace Microsoft.AspNet.Identity
{
    /// <summary>
    ///     Represents a linked login for a user (i.e. a local username/password or a facebook/google account
    /// </summary>
    public sealed class UserLoginInfo
    {
        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="loginProvider"></param>
        /// <param name="providerKey"></param>
        public UserLoginInfo(string loginProvider, string providerKey)
        {
            LoginProvider = loginProvider;
            ProviderKey = providerKey;
        }

        /// <summary>
        ///     Provider for the linked login, i.e. Local, Facebook, Google, etc.
        /// </summary>
        public string LoginProvider { get; set; }

        /// <summary>
        ///     Key for the linked login at the provider
        /// </summary>
        public string ProviderKey { get; set; }
    }
}