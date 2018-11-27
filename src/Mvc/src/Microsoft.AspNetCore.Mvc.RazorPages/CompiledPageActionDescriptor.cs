// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;

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
        /// Gets the list of handler methods for the page. 
        /// </summary>
        public IList<HandlerMethodDescriptor> HandlerMethods { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="TypeInfo"/> of the type that defines handler methods for the page. This can be
        /// the same as <see cref="PageTypeInfo"/> and <see cref="ModelTypeInfo"/> if the page does not have an
        /// explicit model type defined.
        /// </summary>
        public TypeInfo HandlerTypeInfo { get; set; }

        /// <summary>
        /// Gets or sets the declared model <see cref="TypeInfo"/> of the model for the page.
        /// Typically this <see cref="TypeInfo"/> will be the type specified by the @model directive
        /// in the razor page.
        /// </summary>
        public TypeInfo DeclaredModelTypeInfo { get; set; }

        /// <summary>
        /// Gets or sets the runtime model <see cref="TypeInfo"/> of the model for the razor page.
        /// This is the <see cref="TypeInfo"/> that will be used at runtime to instantiate and populate
        /// the model property of the page.
        /// </summary>
        public TypeInfo ModelTypeInfo { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="TypeInfo"/> of the page.
        /// </summary>
        public TypeInfo PageTypeInfo { get; set; }
    }
}
