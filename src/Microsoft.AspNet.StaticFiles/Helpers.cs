// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using System.Globalization;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.StaticFiles
{
    internal static class Helpers
    {
        internal static bool IsGetOrHeadMethod(string method)
        {
            return IsGetMethod(method) || IsHeadMethod(method);
        }

        internal static bool IsGetMethod(string method)
        {
            return string.Equals("GET", method, StringComparison.OrdinalIgnoreCase);
        }

        internal static bool IsHeadMethod(string method)
        {
            return string.Equals("HEAD", method, StringComparison.OrdinalIgnoreCase);
        }

        internal static bool PathEndsInSlash(PathString path)
        {
            return path.Value.EndsWith("/", StringComparison.Ordinal);
        }

        internal static bool TryMatchPath(HttpContext context, PathString matchUrl, bool forDirectory, out PathString subpath)
        {
            var path = context.Request.Path;

            if (forDirectory && !PathEndsInSlash(path))
            {
                path += new PathString("/");
            }

            if (path.StartsWithSegments(matchUrl, out subpath))
            {
                return true;
            }
            return false;
        }

        internal static bool TryParseHttpDate(string dateString, out DateTime parsedDate)
        {
            return DateTime.TryParseExact(dateString, Constants.HttpDateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate);
        }
    }
}
