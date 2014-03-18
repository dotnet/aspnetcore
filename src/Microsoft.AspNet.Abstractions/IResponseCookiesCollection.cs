
namespace Microsoft.AspNet.Abstractions
{
    /// <summary>
    /// A wrapper for the response Set-Cookie header
    /// </summary>
    public interface IResponseCookiesCollection
    {
        /// <summary>
        /// Add a new cookie and value
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        void Append(string key, string value);

        /// <summary>
        /// Add a new cookie
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="options"></param>
        void Append(string key, string value, CookieOptions options);

        /// <summary>
        /// Sets an expired cookie
        /// </summary>
        /// <param name="key"></param>
        void Delete(string key);

        /// <summary>
        /// Sets an expired cookie
        /// </summary>
        /// <param name="key"></param>
        /// <param name="options"></param>
        void Delete(string key, CookieOptions options);
    }
}
