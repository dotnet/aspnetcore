// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.AspNetCore.Mvc.ApplicationParts
{
    /// <summary>
    /// Configures an assembly as a <see cref="CompiledRazorAssemblyPart"/>.
    /// </summary>
    public class CompiledRazorAssemblyApplicationPartFactory : ApplicationPartFactory
    {
        /// <inheritdoc />
        public override IEnumerable<ApplicationPart> GetApplicationParts(Assembly assembly, string configuration)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            yield return new CompiledRazorAssemblyPart(assembly);
        }
    }
}
