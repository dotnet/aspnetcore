// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Routing.Patterns;

namespace Microsoft.AspNetCore.Routing.Template
{
    /// <summary>
    /// A factory used to create <see cref="TemplateBinder"/> instances.
    /// </summary>
    public abstract class TemplateBinderFactory
    {
        /// <summary>
        /// Creates a new <see cref="TemplateBinder"/> from the provided <paramref name="template"/> and
        /// <paramref name="defaults"/>.
        /// </summary>
        /// <param name="template">The route template.</param>
        /// <param name="defaults">A collection of extra default values that do not appear in the route template.</param>
        /// <returns>A <see cref="TemplateBinder"/>.</returns>
        public abstract TemplateBinder Create(RouteTemplate template, RouteValueDictionary defaults);

        /// <summary>
        /// Creates a new <see cref="TemplateBinder"/> from the provided <paramref name="pattern"/>.
        /// </summary>
        /// <param name="pattern">The <see cref="RoutePattern"/>.</param>
        /// <returns>A <see cref="TemplateBinder"/>.</returns>
        public abstract TemplateBinder Create(RoutePattern pattern);
    }
}
