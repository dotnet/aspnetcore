// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

/// <summary>
/// An <see cref="IModelBinderProvider"/> for models which specify an <see cref="IModelBinder"/>
/// using <see cref="BindingInfo.BinderType"/>.
/// </summary>
public class BinderTypeModelBinderProvider : IModelBinderProvider
{
    /// <inheritdoc />
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.BindingInfo.BinderType is Type binderType)
        {
            return new BinderTypeModelBinder(binderType);
        }

        return null;
    }
}
