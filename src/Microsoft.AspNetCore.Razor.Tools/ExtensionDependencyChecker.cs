// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Tools
{
    internal abstract class ExtensionDependencyChecker
    {
        public abstract bool Check(IEnumerable<string> extensionFilePaths);
    }
}