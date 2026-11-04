// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Infrastructure;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels;

internal sealed class TempDataFilterPageApplicationModelProvider : IPageApplicationModelProvider
{
    private readonly TempDataSerializer _tempDataSerializer;

    public TempDataFilterPageApplicationModelProvider(TempDataSerializer tempDataSerializer)
    {
        _tempDataSerializer = tempDataSerializer;
    }

    // The order is set to execute after the DefaultPageApplicationModelProvider.
    public int Order => -1000 + 10;

    public void OnProvidersExecuted(PageApplicationModelProviderContext context)
    {
    }

    public void OnProvidersExecuting(PageApplicationModelProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var pageApplicationModel = context.PageApplicationModel;
        var handlerType = pageApplicationModel.HandlerType.AsType();

        var tempDataProperties = SaveTempDataPropertyFilterBase.GetTempDataProperties(_tempDataSerializer, handlerType);
        if (tempDataProperties == null)
        {
            return;
        }

        var filter = new PageSaveTempDataPropertyFilterFactory(tempDataProperties);
        pageApplicationModel.Filters.Add(filter);
    }
}
