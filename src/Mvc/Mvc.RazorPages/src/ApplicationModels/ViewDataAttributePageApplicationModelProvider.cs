// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Filters;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels;

internal sealed class ViewDataAttributePageApplicationModelProvider : IPageApplicationModelProvider
{
    /// <inheritdoc />
    /// <remarks>This order ensures that <see cref="ViewDataAttributePageApplicationModelProvider"/> runs after the <see cref="DefaultPageApplicationModelProvider"/>.</remarks>
    public int Order => -1000 + 10;

    /// <inheritdoc />
    public void OnProvidersExecuted(PageApplicationModelProviderContext context)
    {
    }

    /// <inheritdoc />
    public void OnProvidersExecuting(PageApplicationModelProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var handlerType = context.PageApplicationModel.HandlerType.AsType();

        var viewDataProperties = ViewDataAttributePropertyProvider.GetViewDataProperties(handlerType);
        if (viewDataProperties == null)
        {
            return;
        }

        var filter = new PageViewDataAttributeFilterFactory(viewDataProperties);
        context.PageApplicationModel.Filters.Add(filter);
    }
}
