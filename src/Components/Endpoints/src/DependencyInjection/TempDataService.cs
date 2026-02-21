// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal sealed partial class TempDataService
{
    private readonly ITempDataProvider _tempDataProvider;

    public TempDataService(ITempDataProvider tempDataProvider)
    {
        _tempDataProvider = tempDataProvider;
    }

    public TempData CreateEmpty(HttpContext httpContext)
    {
        return new TempData(() => Load(httpContext));
    }

    public IDictionary<string, (object? Value, Type? Type)> Load(HttpContext httpContext)
    {
        return _tempDataProvider.LoadTempData(httpContext);
    }

    public void Save(HttpContext httpContext, TempData tempData)
    {
        if (!tempData.WasLoaded)
        {
            return;
        }
        _tempDataProvider.SaveTempData(httpContext, tempData.Save());
    }
}
