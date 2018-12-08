namespace Microsoft.AspNetCore.Authentication.OpenIdConnect
{
    /// <summary>
    /// Configure the client authentication mode to call access_token endpoint
    /// </summary>
    public class OpenIdConnectClientAuthenticationMode
    {
        /// <summary>
        /// Send client id and client secret in the request body 
        /// </summary>
        public static IClientAuthentication Post { get; } = new FormPost();
        /// <summary>
        /// Use basic authorization header
        /// </summary>
        public static IClientAuthentication Basic { get; } = new BasicAuthentication();
    }
}
