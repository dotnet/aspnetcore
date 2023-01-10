// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels;

internal sealed class AutoValidateAntiforgeryPageApplicationModelProvider : IPageApplicationModelProvider
{
    // The order is set to execute after the DefaultPageApplicationModelProvider.
    public int Order => -1000 + 10;

    public void OnProvidersExecuted(PageApplicationModelProviderContext context)
    {
    }

    public void OnProvidersExecuting(PageApplicationModelProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var pageApplicationModel = context.PageApplicationModel;

        // ValidateAntiforgeryTokenAttribute relies on order to determine if it's the effective policy.
        // When two antiforgery filters of the same order are added to the application model, the effective policy is determined
        // by whatever appears later in the list (closest to the action). This causes filters listed on the model to be pre-empted
        // by the one added here. We'll resolve this unusual behavior by skipping the addition of the AutoValidateAntiforgeryTokenAttribute
        // when another already exists.
        if (!pageApplicationModel.Filters.OfType<IAntiforgeryPolicy>().Any())
        {
            // Always require an antiforgery token on post
            pageApplicationModel.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
        }
    }
}
