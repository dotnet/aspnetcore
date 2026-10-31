// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.JsonPatch.SystemTextJson.Exceptions;
using Microsoft.AspNetCore.JsonPatch.SystemTextJson;

namespace Microsoft.AspNetCore.JsonPatch.SystemTextJson.Internal;

internal static class PathHelpers
{
    internal static string NormalizePath(string path)
    {
        // An empty string on its own is valid, and is different from "/".
        // So, we never want to normalize an empty string.
        if (path.Length > 0 && !path.StartsWith('/'))
        {
            return $"/{path}";
        }

        return path;
    }
}
