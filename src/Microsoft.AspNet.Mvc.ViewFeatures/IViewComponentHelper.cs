// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Html.Abstractions;

namespace Microsoft.AspNet.Mvc
{
    public interface IViewComponentHelper
    {
        IHtmlContent Invoke(string name, params object[] args);

        IHtmlContent Invoke(Type componentType, params object[] args);

        void RenderInvoke(string name, params object[] args);

        void RenderInvoke(Type componentType, params object[] args);

        Task<IHtmlContent> InvokeAsync(string name, params object[] args);

        Task<IHtmlContent> InvokeAsync(Type componentType, params object[] args);

        Task RenderInvokeAsync(string name, params object[] args);

        Task RenderInvokeAsync(Type componentType, params object[] args);
    }
}
