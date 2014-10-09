// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.FileSystems;

namespace Microsoft.AspNet.Mvc.Razor
{
    public interface ICompilerCache
    {
        CompilationResult GetOrAdd([NotNull] RelativeFileInfo fileInfo,
                                   bool enableInstrumentation,
                                   [NotNull] Func<CompilationResult> compile);
    }
}