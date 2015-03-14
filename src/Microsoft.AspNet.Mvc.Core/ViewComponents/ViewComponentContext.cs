// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Microsoft.AspNet.Mvc.ViewComponents;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// A context for View Components.
    /// </summary>
    public class ViewComponentContext
    {
        /// <summary>
        /// Creates a new <see cref="ViewComponentContext"/>.
        /// </summary>
        /// <param name="viewComponentDescriptor">
        /// The <see cref="ViewComponentContext"/> for the View Component being invoked.
        /// </param>
        /// <param name="arguments">The View Component arguments.</param>
        /// <param name="viewContext">The <see cref="ViewContext"/>.</param>
        /// <param name="writer">The <see cref="TextWriter"/> for writing output.</param>
        public ViewComponentContext(
            [NotNull] ViewComponentDescriptor viewComponentDescriptor,
            [NotNull] object[] arguments,
            [NotNull] ViewContext viewContext,
            [NotNull] TextWriter writer)
        {
            ViewComponentDescriptor = viewComponentDescriptor;
            Arguments = arguments;
            ViewContext = viewContext;
            Writer = writer;
        }

        /// <summary>
        /// Gets the View Component arguments. 
        /// </summary>
        public object[] Arguments { get; }

        /// <summary>
        /// Gets the <see cref="ViewComponentDescriptor"/> for the View Component being invoked.
        /// </summary>
        public ViewComponentDescriptor ViewComponentDescriptor { get; }

        /// <summary>
        /// Gets the <see cref="ViewContext"/>.
        /// </summary>
        public ViewContext ViewContext { get; }

        /// <summary>
        /// Gets the <see cref="TextWriter"/> for writing output.
        /// </summary>
        /// <remarks>
        /// <see cref="IViewComponentHelper.Invoke(string, object[])"/> or a similar overload is used to invoke the
        /// View Component, then <see cref="Writer"/> will be different than <see cref="ViewContext.Writer"/>.
        /// </remarks>
        public TextWriter Writer { get; }
    }
}
