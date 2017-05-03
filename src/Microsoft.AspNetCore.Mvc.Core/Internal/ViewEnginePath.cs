// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    public static class ViewEnginePath
    {
        private const string CurrentDirectoryToken = ".";
        private const string ParentDirectoryToken = "..";
        private static readonly char[] _pathSeparators = new[] { '/', '\\' };

        public static string CombinePath(string first, string second)
        {
            Debug.Assert(!string.IsNullOrEmpty(first));

            if (second.StartsWith("/", StringComparison.Ordinal))
            {
                // "second" is already an app-rooted path. Return it as-is.
                return second;
            }

            string result;
           
            // Get directory name (including final slash) but do not use Path.GetDirectoryName() to preserve path
            // normalization.
            var index = first.LastIndexOf('/');
            Debug.Assert(index >= 0);

            if (index == first.Length - 1)
            {
                // If the first ends in a trailing slash e.g. "/Home/", assume it's a directory.
                result = first + second;
            }
            else
            {
                result = first.Substring(0, index + 1) + second;
            }

            return ResolvePath(result);
        }

        public static string ResolvePath(string path)
        {
            if (!RequiresPathResolution(path))
            {
                return path;
            }

            var pathSegments = new List<StringSegment>();
            var tokenizer = new StringTokenizer(path, _pathSeparators);
            foreach (var segment in tokenizer)
            {
                if (segment.Length == 0)
                {
                    // Ignore multiple directory separators
                    continue;
                }
                if (segment.Equals(ParentDirectoryToken, StringComparison.Ordinal))
                {
                    if (pathSegments.Count == 0)
                    {
                        // Don't resolve the path if we ever escape the file system root. We can't reason about it in a
                        // consistent way.
                        return path;
                    }
                    pathSegments.RemoveAt(pathSegments.Count - 1);
                }
                else if (segment.Equals(CurrentDirectoryToken, StringComparison.Ordinal))
                {
                    // We already have the current directory
                    continue;
                }
                else
                {
                    pathSegments.Add(segment);
                }
            }

            var builder = new StringBuilder();
            for (var i = 0; i < pathSegments.Count; i++)
            {
                var segment = pathSegments[i];
                builder.Append('/');
                builder.Append(segment.Buffer, segment.Offset, segment.Length);
            }

            return builder.ToString();
        }

        private static bool RequiresPathResolution(string path)
        {
            return path.IndexOf(ParentDirectoryToken, StringComparison.Ordinal) != -1 ||
                path.IndexOf(CurrentDirectoryToken, StringComparison.Ordinal) != -1;
        }
    }
}
