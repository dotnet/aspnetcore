// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Razor.Hosting;

namespace Microsoft.AspNetCore.Mvc.ApplicationParts
{
    /// <summary>
    /// An <see cref="ApplicationPart"/> for compiled Razor assemblies.
    /// </summary>
    public class CompiledRazorAssemblyPart : ApplicationPart, IRazorCompiledItemProvider
    {
        /// <summary>
        /// Initializes a new instance of <see cref="CompiledRazorAssemblyPart"/>.
        /// </summary>
        /// <param name="assembly">The <see cref="System.Reflection.Assembly"/></param>
        public CompiledRazorAssemblyPart(Assembly assembly)
        {
            Assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
        }

        /// <summary>
        /// Gets the <see cref="System.Reflection.Assembly"/>.
        /// </summary>
        public Assembly Assembly { get; }

        /// <inheritdoc />
        public override string Name => Assembly.GetName().Name;

        IEnumerable<RazorCompiledItem> IRazorCompiledItemProvider.CompiledItems
        {
            get
            {
                var loader = new RazorCompiledItemLoader();
                return loader.LoadItems(Assembly);
            }
        }
    }
}
