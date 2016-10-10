// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.DotNet.ProjectModel.Files;

namespace Microsoft.Extensions.ProjectModel
{
    internal static class IncludeContextExtensions
    {
        public static IEnumerable<string> ResolveFiles(this IncludeContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return IncludeFilesResolver
                .GetIncludeFiles(context, "/", diagnostics: null)
                .Select(f => f.SourcePath);
        }
    }
}
