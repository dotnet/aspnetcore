// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;

namespace NuGet
{
    internal static class PathUtility
    {
        public static bool IsChildOfDirectory(string dir, string candidate)
        {
            if (dir == null)
            {
                throw new ArgumentNullException(nameof(dir));
            }
            if (candidate == null)
            {
                throw new ArgumentNullException(nameof(candidate));
            }
            dir = Path.GetFullPath(dir);
            dir = EnsureTrailingSlash(dir);
            candidate = Path.GetFullPath(candidate);
            return candidate.StartsWith(dir, StringComparison.OrdinalIgnoreCase);
        }

        public static string EnsureTrailingSlash(string path)
        {
            return EnsureTrailingCharacter(path, Path.DirectorySeparatorChar);
        }

        public static string EnsureTrailingForwardSlash(string path)
        {
            return EnsureTrailingCharacter(path, '/');
        }

        private static string EnsureTrailingCharacter(string path, char trailingCharacter)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            // if the path is empty, we want to return the original string instead of a single trailing character.
            if (path.Length == 0 || path[path.Length - 1] == trailingCharacter)
            {
                return path;
            }

            return path + trailingCharacter;
        }

        public static void EnsureParentDirectory(string filePath)
        {
            string directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        /// <summary>
        /// Returns path2 relative to path1, with Path.DirectorySeparatorChar as separator
        /// </summary>
        public static string GetRelativePath(string path1, string path2)
        {
            return GetRelativePath(path1, path2, Path.DirectorySeparatorChar);
        }

        /// <summary>
        /// Returns path2 relative to path1, with given path separator
        /// </summary>
        public static string GetRelativePath(string path1, string path2, char separator)
        {
            if (string.IsNullOrEmpty(path1))
            {
                throw new ArgumentException("Path must have a value", nameof(path1));
            }

            if (string.IsNullOrEmpty(path2))
            {
                throw new ArgumentException("Path must have a value", nameof(path2));
            }

            StringComparison compare;
            if (Microsoft.Dnx.Runtime.RuntimeEnvironmentHelper.IsWindows)
            {
                compare = StringComparison.OrdinalIgnoreCase;
                // check if paths are on the same volume
                if (!string.Equals(Path.GetPathRoot(path1), Path.GetPathRoot(path2)))
                {
                    // on different volumes, "relative" path is just path2
                    return path2;
                }
            }
            else
            {
                compare = StringComparison.Ordinal;
            }

            var index = 0;
            var path1Segments = path1.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var path2Segments = path2.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            // if path1 does not end with / it is assumed the end is not a directory
            // we will assume that is isn't a directory by ignoring the last split
            var len1 = path1Segments.Length - 1;
            var len2 = path2Segments.Length;

            // find largest common absolute path between both paths
            var min = Math.Min(len1, len2);
            while (min > index)
            {
                if (!string.Equals(path1Segments[index], path2Segments[index], compare))
                {
                    break;
                }
                // Handle scenarios where folder and file have same name (only if os supports same name for file and directory)
                // e.g. /file/name /file/name/app
                else if ((len1 == index && len2 > index + 1) || (len1 > index && len2 == index + 1))
                {
                    break;
                }
                ++index;
            }

            var path = "";

            // check if path2 ends with a non-directory separator and if path1 has the same non-directory at the end
            if (len1 + 1 == len2 && !string.IsNullOrEmpty(path1Segments[index]) &&
                string.Equals(path1Segments[index], path2Segments[index], compare))
            {
                return path;
            }

            for (var i = index; len1 > i; ++i)
            {
                path += ".." + separator;
            }
            for (var i = index; len2 - 1 > i; ++i)
            {
                path += path2Segments[i] + separator;
            }
            // if path2 doesn't end with an empty string it means it ended with a non-directory name, so we add it back
            if (!string.IsNullOrEmpty(path2Segments[len2 - 1]))
            {
                path += path2Segments[len2 - 1];
            }

            return path;
        }

        public static string GetAbsolutePath(string basePath, string relativePath)
        {
            if (basePath == null)
            {
                throw new ArgumentNullException(nameof(basePath));
            }

            if (relativePath == null)
            {
                throw new ArgumentNullException(nameof(relativePath));
            }

            Uri resultUri = new Uri(new Uri(basePath), new Uri(relativePath, UriKind.Relative));
            return resultUri.LocalPath;
        }

        public static string GetDirectoryName(string path)
        {
            path = path.TrimEnd(Path.DirectorySeparatorChar);
            return path.Substring(Path.GetDirectoryName(path).Length).Trim(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        public static string GetPathWithForwardSlashes(string path)
        {
            return path.Replace('\\', '/');
        }

        public static string GetPathWithBackSlashes(string path)
        {
            return path.Replace('/', '\\');
        }

        public static string GetPathWithDirectorySeparator(string path)
        {
            if (Path.DirectorySeparatorChar == '/')
            {
                return GetPathWithForwardSlashes(path);
            }
            else
            {
                return GetPathWithBackSlashes(path);
            }
        }
    }
}