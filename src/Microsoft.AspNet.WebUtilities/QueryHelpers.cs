using System;

namespace Microsoft.AspNet.WebUtilities
{
    public static class QueryHelpers
    {
        /// <summary>
        /// Append the given query key and value to the URI.
        /// </summary>
        /// <param name="uri">The base URI.</param>
        /// <param name="name">The name of the query key.</param>
        /// <param name="value">The query value.</param>
        /// <returns>The combined result.</returns>
        public static string AddQueryString([NotNull] string uri, [NotNull] string name, [NotNull] string value)
        {
            bool hasQuery = uri.IndexOf('?') != -1;
            return uri + (hasQuery ? "&" : "?") + Uri.EscapeDataString(name) + "=" + Uri.EscapeDataString(value);
        }
    }
}