// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    internal sealed class ProjectExtensibilityAssembly : IEquatable<ProjectExtensibilityAssembly>
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

        public bool Equals(ProjectExtensibilityAssembly other)
        {
            if (other == null)
            {
                return false;
            }

            return Identity.Equals(other.Identity);
        }

        public override int GetHashCode()
        {
            return Identity.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj as ProjectExtensibilityAssembly);
        }
    }
}
