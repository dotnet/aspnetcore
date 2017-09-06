// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    internal sealed class ProjectExtensibilityAssembly
    {
        public ProjectExtensibilityAssembly(AssemblyIdentity identity)
        {
            if (identity == null)
            {
                throw new ArgumentNullException(nameof(identity));
            }

            Identity = identity;
        }

        public AssemblyIdentity Identity { get; }
    }
}
