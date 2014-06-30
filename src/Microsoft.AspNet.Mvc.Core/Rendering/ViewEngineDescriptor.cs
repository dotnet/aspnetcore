// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc.Core;

namespace Microsoft.AspNet.Mvc.Rendering
{
    /// <summary>
    /// Encapsulates information that describes an <see cref="IViewEngine"/>.
    /// </summary>
    public class ViewEngineDescriptor
    {
        /// <summary>
        /// Creates a new instance of <see cref="ViewEngineDescriptor"/>.
        /// </summary>
        /// <param name="type">The <see cref="IViewEngine/> type that the descriptor represents.</param>
        public ViewEngineDescriptor([NotNull] Type type)
        {
            var viewEngineType = typeof(IViewEngine);
            if (!viewEngineType.IsAssignableFrom(type))
            {
                var message = Resources.FormatTypeMustDeriveFromType(type.FullName, viewEngineType.FullName);
                throw new ArgumentException(message, "type");
            }

            ViewEngineType = type;
        }

        /// <summary>
        /// Creates a new instance of <see cref="ViewEngineDescriptor"/>.
        /// </summary>
        /// <param name="viewEngine">An instance of <see cref="IViewEngine"/> that the descriptor represents.</param>
        public ViewEngineDescriptor([NotNull] IViewEngine viewEngine)
        {
            ViewEngine = viewEngine;
            ViewEngineType = viewEngine.GetType();
        }

        /// <summary>
        /// Gets the type of the <see cref="IViewEngine"/>.
        /// </summary>
        public Type ViewEngineType
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the instance of the <see cref="IViewEngine"/>.
        /// </summary>
        public IViewEngine ViewEngine
        {
            get;
            private set;
        }
    }
}