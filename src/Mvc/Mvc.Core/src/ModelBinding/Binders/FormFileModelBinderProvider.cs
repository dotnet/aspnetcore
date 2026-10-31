// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

/// <summary>
/// An <see cref="IModelBinderProvider"/> for <see cref="IFormFile"/>, collections
/// of <see cref="IFormFile"/>, and <see cref="IFormFileCollection"/>.
/// </summary>
public class FormFileModelBinderProvider : IModelBinderProvider
{
    /// <inheritdoc />
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Note: This condition needs to be kept in sync with ApiBehaviorApplicationModelProvider.
        var modelType = context.Metadata.ModelType;
        if (modelType == typeof(IFormFile) ||
            modelType == typeof(IFormFileCollection) ||
            typeof(IEnumerable<IFormFile>).IsAssignableFrom(modelType))
        {
            var loggerFactory = context.Services.GetRequiredService<ILoggerFactory>();
            return new FormFileModelBinder(loggerFactory);
        }

        return null;
    }
}
