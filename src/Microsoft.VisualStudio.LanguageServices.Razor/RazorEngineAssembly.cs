// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.LanguageServices.Razor
{
    internal struct RazorEngineAssembly
    {
        public RazorEngineAssembly(AssemblyIdentity identity, string filePath)
        {
            if (identity == null)
            {
                throw new ArgumentNullException(nameof(identity));
            }

            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            Identity = identity;
            FilePath = filePath;
        }

        public string FilePath { get; }

        public AssemblyIdentity Identity { get; }
    }
}
