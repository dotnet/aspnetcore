// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Mvc.Internal
{
    public static class UrlUtility
    {
        /// <summary>
        /// Returns a value that indicates whether the URL is local. An URL with an absolute path is considered local
        /// if it does not have a host/authority part. URLs using the virtual paths ('~/') are also local.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <returns><c>true</c> if the URL is local; otherwise, <c>false</c>.</returns>
        /// <example>
        /// <para>
        /// For example, the following URLs are considered local:
        /// /Views/Default/Index.html
        /// ~/Index.html
        /// </para>
        /// <para>
        /// The following URLs are non-local:
        /// ../Index.html
        /// http://www.contoso.com/
        /// http://localhost/Index.html
        /// </para>
        /// </example>
        public static bool IsLocalUrl(string url)
        {
            return
                !string.IsNullOrEmpty(url) &&

                // Allows "/" or "/foo" but not "//" or "/\".
                ((url[0] == '/' && (url.Length == 1 || (url[1] != '/' && url[1] != '\\'))) ||

                // Allows "~/" or "~/foo".
                (url.Length > 1 && url[0] == '~' && url[1] == '/'));
        }
    }
}