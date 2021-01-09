// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures
{
    /// <summary>
    /// IComponentRenderer which can be used to render a component.
    /// </summary>
    public interface IComponentRenderer
    {
        /// <summary>
        /// Renders a <paramref name="componentType"/> with <paramref name="parameters"/>
        /// and returns <see cref="IHtmlContent"/>. 
        /// </summary>
        /// <param name="viewContext">The <see cref="ViewContext"/> in which the component is rendered</param>
        /// <param name="componentType">The <see cref="Type"/> of the component to render</param>
        /// <param name="renderMode">The <see cref="RenderMode"/></param>
        /// <param name="parameters">The component parameters</param>
        /// <returns></returns>
        Task<IHtmlContent> RenderComponentAsync(
            ViewContext viewContext,
            Type componentType,
            RenderMode renderMode,
            object parameters);
    }
}
