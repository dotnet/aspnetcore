// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.JsonPatch.Exceptions;

namespace Microsoft.AspNetCore.JsonPatch.Internal;

internal static class PathHelpers
{
    internal static string ValidateAndNormalizePath(string path)
    {
        // check for most common path errors on create.  This is not
        // absolutely necessary, but it allows us to already catch mistakes
        // on creation of the patch document rather than on execute.

        if (path.Contains("//"))
        {
            throw new JsonPatchException(Resources.FormatInvalidValueForPath(path), null);
        }

        if (!path.StartsWith("/", StringComparison.Ordinal))
        {
            return "/" + path;
        }
        else
        {
            return path;
        }
    }
}
