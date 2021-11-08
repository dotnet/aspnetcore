// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Microsoft.AspNetCore.Mvc.RazorPages;

public abstract class PageModel
{
    public IUrlHelper Url { get; set; }

    public Page Page => PageContext?.Page;

    public PageContext PageContext { get; set; }

    public ViewContext ViewContext => PageContext;

    public ITempDataDictionary TempData { get; }

    public ViewDataDictionary ViewData { get; }

    protected internal Task<bool> TryUpdateModelAsync<TModel>(TModel model)
        where TModel : class
    {
        throw new NotImplementedException();
    }

    protected internal Task<bool> TryUpdateModelAsync<TModel>(TModel model, string name)
        where TModel : class
    {
        throw new NotImplementedException();
    }

    protected internal RedirectResult Redirect(string url) => throw new NotImplementedException();

    public virtual bool TryValidateModel(object model) => throw new NotImplementedException();

    public virtual bool TryValidateModel(object model, string name) => throw new NotImplementedException();
}
