// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Reflection;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

/// <summary>
/// An <see cref="IModelBinderProvider"/> for binding from the <see cref="IServiceProvider"/>.
/// </summary>
public class ServicesModelBinderProvider : IModelBinderProvider
{
    private readonly NullabilityInfoContext _nullabilityContext = new();

    /// <inheritdoc />
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (context.BindingInfo.BindingSource != null &&
            context.BindingInfo.BindingSource.CanAcceptDataFrom(BindingSource.Services))
        {
            return new ServicesModelBinder()
            {
                IsOptionalParameter = IsOptionalParameter(context.Metadata.Identity.ParameterInfo!)
            };
        }

        return null;
    }

    internal bool IsOptionalParameter(ParameterInfo parameterInfo) =>
        parameterInfo.HasDefaultValue ||
        _nullabilityContext.Create(parameterInfo).ReadState == NullabilityState.Nullable;
}
