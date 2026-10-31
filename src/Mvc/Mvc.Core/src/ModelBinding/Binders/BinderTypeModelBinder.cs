// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

/// <summary>
/// An <see cref="IModelBinder"/> for models which specify an <see cref="IModelBinder"/> using
/// <see cref="BindingInfo.BinderType"/>.
/// </summary>
public class BinderTypeModelBinder : IModelBinder
{
    private readonly ObjectFactory _factory;

    /// <summary>
    /// Creates a new <see cref="BinderTypeModelBinder"/>.
    /// </summary>
    /// <param name="binderType">The <see cref="Type"/> of the <see cref="IModelBinder"/>.</param>
    public BinderTypeModelBinder(Type binderType)
    {
        ArgumentNullException.ThrowIfNull(binderType);

        if (!typeof(IModelBinder).IsAssignableFrom(binderType))
        {
            throw new ArgumentException(
                Resources.FormatBinderType_MustBeIModelBinder(
                    binderType.FullName,
                    typeof(IModelBinder).FullName),
                nameof(binderType));
        }

        _factory = ActivatorUtilities.CreateFactory(binderType, Type.EmptyTypes);
    }

    /// <inheritdoc />
    public async Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

        var requestServices = bindingContext.HttpContext.RequestServices;
        var binder = (IModelBinder)_factory(requestServices, arguments: null);

        await binder.BindModelAsync(bindingContext);
    }
}
