// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Reflection;

namespace Microsoft.AspNetCore.Mvc.RazorPages
{
    /// <summary>
    /// A <see cref="PageActionDescriptor"/> for a compiled Razor page.
    /// </summary>
    public class CompiledPageActionDescriptor : PageActionDescriptor
    {
        /// <summary>
        /// Initializes an empty <see cref="CompiledPageActionDescriptor"/>.
        /// </summary>
        public CompiledPageActionDescriptor()
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="CompiledPageActionDescriptor"/>
        /// from the specified <paramref name="actionDescriptor"/> instance.
        /// </summary>
        /// <param name="actionDescriptor">The <see cref="PageActionDescriptor"/>.</param>
        public CompiledPageActionDescriptor(PageActionDescriptor actionDescriptor)
            : base(actionDescriptor)
        {
        }

        /// <summary>
        /// Gets or sets the <see cref="TypeInfo"/> of the page.
        /// </summary>
        public TypeInfo PageTypeInfo { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="TypeInfo"/> of the model.
        /// </summary>
        public TypeInfo ModelTypeInfo { get; set; }
    }
}
