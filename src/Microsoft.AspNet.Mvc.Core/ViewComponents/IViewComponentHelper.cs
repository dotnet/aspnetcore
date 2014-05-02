// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Rendering;

namespace Microsoft.AspNet.Mvc
{
    public interface IViewComponentHelper
    {
        HtmlString Invoke(string name, params object[] args);

        HtmlString Invoke(Type componentType, params object[] args);

        void RenderInvoke(string name, params object[] args);

        void RenderInvoke(Type componentType, params object[] args);

        Task<HtmlString> InvokeAsync(string name, params object[] args);

        Task<HtmlString> InvokeAsync(Type componentType, params object[] args);

        Task RenderInvokeAsync(string name, params object[] args);

        Task RenderInvokeAsync(Type componentType, params object[] args);
    }
}
