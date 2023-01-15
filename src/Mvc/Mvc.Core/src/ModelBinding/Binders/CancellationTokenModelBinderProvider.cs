// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

/// <summary>
/// An <see cref="IModelBinderProvider"/> for <see cref="CancellationToken"/>.
/// </summary>
public class CancellationTokenModelBinderProvider : IModelBinderProvider
{
    // CancellationTokenModelBinder does not have any state. Re-use the same instance for binding.

    private readonly CancellationTokenModelBinder _modelBinder = new();

    /// <inheritdoc />
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Metadata.ModelType == typeof(CancellationToken))
        {
            return _modelBinder;
        }

        return null;
    }
}
