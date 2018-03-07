// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;

namespace Microsoft.AspNetCore.Mvc.ApplicationParts
{
    public class CompiledRazorAssemblyPart : ApplicationPart
    {
        public CompiledRazorAssemblyPart(Assembly assembly)
        {
            Assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
        }

        public Assembly Assembly { get; }

        public override string Name => Assembly.GetName().Name;
    }
}
