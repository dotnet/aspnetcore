// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Encapsulates information that describes an <see cref="IViewEngine"/>.
    /// </summary>
    public class ViewEngineDescriptor
    {
        /// <summary>
        /// Creates a new instance of <see cref="ViewEngineDescriptor"/>.
        /// </summary>
        /// <param name="type">The <see cref="IViewEngine"/> type that the descriptor represents.</param>
        public ViewEngineDescriptor([NotNull] Type type)
        {
            if (!typeof(IViewEngine).IsAssignableFrom(type))
            {
                var message = Resources.FormatTypeMustDeriveFromType(type.FullName, typeof(IViewEngine).FullName);
                throw new ArgumentException(message, nameof(type));
            }

            ViewEngineType = type;
        }

        /// <summary>
        /// Creates a new instance of <see cref="ViewEngineDescriptor"/> using the specified type.
        /// </summary>
        /// <param name="viewEngine">An instance of <see cref="IViewEngine"/> that the descriptor represents.</param>
        public ViewEngineDescriptor([NotNull] IViewEngine viewEngine)
        {
            ViewEngine = viewEngine;
            ViewEngineType = viewEngine.GetType();
        }

        /// <summary>
        /// Gets the type of the <see cref="IViewEngine"/> described by this <see cref="ViewEngineDescriptor"/>.
        /// </summary>
        public Type ViewEngineType { get; }

        /// <summary>
        /// Gets the <see cref="IViewEngine"/> instance described by this <see cref="ViewEngineDescriptor"/>.
        /// </summary>
        public IViewEngine ViewEngine { get; }
    }
}