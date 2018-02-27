// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.JsonPatch.Exceptions;
using System;

namespace Microsoft.AspNetCore.JsonPatch.Internal
{
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
}
