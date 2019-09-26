// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
