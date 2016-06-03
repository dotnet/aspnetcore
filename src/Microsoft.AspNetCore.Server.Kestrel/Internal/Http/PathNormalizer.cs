// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Buffers;

namespace Microsoft.AspNetCore.Server.Kestrel.Internal.Http
{
    public static class PathNormalizer
    {
        public static string RemoveDotSegments(string path)
        {
            if (ContainsDotSegments(path))
            {
                var normalizedChars = ArrayPool<char>.Shared.Rent(path.Length);
                var normalizedIndex = normalizedChars.Length;
                var pathIndex = path.Length - 1;
                var skipSegments = 0;

                while (pathIndex >= 0)
                {
                    if (pathIndex >= 2 && path[pathIndex] == '.' && path[pathIndex - 1] == '.' && path[pathIndex - 2] == '/')
                    {
                        if (normalizedIndex == normalizedChars.Length || normalizedChars[normalizedIndex] != '/')
                        {
                            normalizedChars[--normalizedIndex] = '/';
                        }

                        skipSegments++;
                        pathIndex -= 3;
                    }
                    else if (pathIndex >= 1 && path[pathIndex] == '.' && path[pathIndex - 1] == '/')
                    {
                        pathIndex -= 2;
                    }
                    else
                    {
                        while (pathIndex >= 0)
                        {
                            var lastChar = path[pathIndex];

                            if (skipSegments == 0)
                            {
                                normalizedChars[--normalizedIndex] = lastChar;
                            }

                            pathIndex--;

                            if (lastChar == '/')
                            {
                                break;
                            }
                        }

                        if (skipSegments > 0)
                        {
                            skipSegments--;
                        }
                    }
                }

                path = new string(normalizedChars, normalizedIndex, normalizedChars.Length - normalizedIndex);
                ArrayPool<char>.Shared.Return(normalizedChars);
            }

            return path;
        }

        private unsafe static bool ContainsDotSegments(string path)
        {
            fixed (char* ptr = path)
            {
                char* end = ptr + path.Length;

                for (char* p = ptr; p < end; p++)
                {
                    if (*p == '/')
                    {
                        p++;
                    }

                    if (p == end)
                    {
                        return false;
                    }

                    if (*p == '.')
                    {
                        p++;

                        if (p == end)
                        {
                            return true;
                        }

                        if (*p == '.')
                        {
                            p++;

                            if (p == end)
                            {
                                return true;
                            }

                            if (*p == '/')
                            {
                                return true;
                            }
                        }
                        else if (*p == '/')
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}
