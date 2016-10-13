// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.DotNet.ProjectModel.Files;

namespace Microsoft.DotNet.Watcher.Internal
{
    internal static class IncludeContextExtensions
    {
        public static IEnumerable<string> ResolveFiles(this IncludeContext context)
        {
            Ensure.NotNull(context, nameof(context));

            return IncludeFilesResolver
                .GetIncludeFiles(context, "/", diagnostics: null)
                .Select(f => f.SourcePath);
        }
    }
}
