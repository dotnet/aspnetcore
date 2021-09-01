// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Microsoft.AspNetCore.Mvc.Razor
{
    public abstract class RazorPage<TModel> : RazorPage
    {
        public TModel Model { get; }

        public ViewDataDictionary<TModel> ViewData { get; set; }
    }
}
