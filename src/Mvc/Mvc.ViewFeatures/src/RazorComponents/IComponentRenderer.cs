// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures
{
    internal interface IComponentRenderer
    {
        Task<IHtmlContent> RenderComponentAsync(
            ViewContext viewContext,
            Type componentType,
            RenderMode renderMode,
            object parameters);
    }
}
