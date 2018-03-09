// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.AspNetCore.Mvc.ApplicationParts
{
    /// <summary>
    /// An <see cref="AssemblyPart"/> that was added by an assembly that referenced it through the use
    /// of an assembly metadata attribute.
    /// </summary>
    public class AdditionalAssemblyPart : AssemblyPart, ICompilationReferencesProvider, IApplicationPartTypeProvider
    {
        /// <inheritdoc />
        public AdditionalAssemblyPart(Assembly assembly) : base(assembly)
        {
        }

        IEnumerable<string> ICompilationReferencesProvider.GetReferencePaths() => Array.Empty<string>();

        IEnumerable<TypeInfo> IApplicationPartTypeProvider.Types => Array.Empty<TypeInfo>();
    }
}
