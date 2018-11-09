// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Mvc.Razor
{
    internal static class ViewPath
    {
        public static string NormalizePath(string path)
        {
            var addLeadingSlash = path[0] != '\\' && path[0] != '/';
            var transformSlashes = path.IndexOf('\\') != -1;

            if (!addLeadingSlash && !transformSlashes)
            {
                return path;
            }

            var length = path.Length;
            if (addLeadingSlash)
            {
                length++;
            }

            var builder = new InplaceStringBuilder(length);
            if (addLeadingSlash)
            {
                builder.Append('/');
            }

            for (var i = 0; i < path.Length; i++)
            {
                var ch = path[i];
                if (ch == '\\')
                {
                    ch = '/';
                }
                builder.Append(ch);
            }

            return builder.ToString();
        }
    }
}
