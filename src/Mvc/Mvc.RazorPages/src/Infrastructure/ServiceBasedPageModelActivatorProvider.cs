// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;

/// <summary>
/// <see cref="IPageActivatorProvider"/> that uses type activation to create Razor Page instances.
/// </summary>
public class ServiceBasedPageModelActivatorProvider : IPageModelActivatorProvider
{
    /// <inheritdoc/>
    public Func<PageContext, object> CreateActivator(CompiledPageActionDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        var modelType = descriptor.ModelTypeInfo?.AsType();
        if (modelType == null)
        {
            throw new ArgumentException(Resources.FormatPropertyOfTypeCannotBeNull(
                nameof(descriptor.ModelTypeInfo),
                nameof(descriptor)),
                nameof(descriptor));
        }

        return context =>
        {
            return context.HttpContext.RequestServices.GetRequiredService(modelType);
        };
    }

    /// <inheritdoc/>
    public Action<PageContext, object>? CreateReleaser(CompiledPageActionDescriptor descriptor)
    {
        return null;
    }
}

