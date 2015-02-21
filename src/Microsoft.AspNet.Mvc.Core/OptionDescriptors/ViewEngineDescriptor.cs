// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.OptionDescriptors
{
    /// <summary>
    /// Encapsulates information that describes an <see cref="IViewEngine"/>.
    /// </summary>
    public class ViewEngineDescriptor : OptionDescriptor<IViewEngine>
    {
        /// <summary>
        /// Creates a new instance of <see cref="ViewEngineDescriptor"/>.
        /// </summary>
        /// <param name="type">The <see cref="IViewEngine"/> type that the descriptor represents.</param>
        public ViewEngineDescriptor([NotNull] Type type)
            : base(type)
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="ViewEngineDescriptor"/> using the specified type.
        /// </summary>
        /// <param name="viewEngine">An instance of <see cref="IViewEngine"/> that the descriptor represents.</param>
        public ViewEngineDescriptor([NotNull] IViewEngine viewEngine)
            : base(viewEngine)
        {
        }
    }
}