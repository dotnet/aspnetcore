// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;

namespace Microsoft.AspNetCore.Mvc
{
    public interface IViewComponentHelper
    {
        Task<IHtmlContent> InvokeAsync(string name, object arguments);

        Task<IHtmlContent> InvokeAsync(Type componentType, object arguments);
    }
}
