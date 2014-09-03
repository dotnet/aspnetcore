// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc.OptionDescriptors;

namespace Microsoft.AspNet.Mvc.Razor.OptionDescriptors
{
    /// <summary>
    /// Encapsulates information that describes an <see cref="IViewLocationExpander"/>.
    /// </summary>
    public class ViewLocationExpanderDescriptor : OptionDescriptor<IViewLocationExpander>
    {
        /// <summary>
        /// Creates a new instance of <see cref="ViewLocationExpanderDescriptor"/>.
        /// </summary>
        /// <param name="type">A <see cref="IViewLocationExpander"/> type that the descriptor represents.
        /// </param>
        public ViewLocationExpanderDescriptor([NotNull] Type type)
            : base(type)
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="ViewLocationExpanderDescriptor"/>.
        /// </summary>
        /// <param name="viewLocationExpander">An instance of <see cref="IViewLocationExpander"/>
        /// that the descriptor represents.</param>
        public ViewLocationExpanderDescriptor([NotNull] IViewLocationExpander viewLocationExpander)
            : base(viewLocationExpander)
        {
        }
    }
}