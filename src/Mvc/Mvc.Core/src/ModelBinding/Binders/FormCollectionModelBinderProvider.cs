// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

/// <summary>
/// An <see cref="IModelBinderProvider"/> for <see cref="IFormCollection"/>.
/// </summary>
public class FormCollectionModelBinderProvider : IModelBinderProvider
{
    /// <inheritdoc />
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var modelType = context.Metadata.ModelType;

        if (typeof(FormCollection).IsAssignableFrom(modelType))
        {
            throw new InvalidOperationException(
                Resources.FormatFormCollectionModelBinder_CannotBindToFormCollection(
                    typeof(FormCollectionModelBinder).FullName,
                    modelType.FullName,
                    typeof(IFormCollection).FullName));
        }

        if (modelType == typeof(IFormCollection))
        {
            var loggerFactory = context.Services.GetRequiredService<ILoggerFactory>();
            return new FormCollectionModelBinder(loggerFactory);
        }

        return null;
    }
}
