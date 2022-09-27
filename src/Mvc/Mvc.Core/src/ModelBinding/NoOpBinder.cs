// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace Microsoft.AspNetCore.Mvc.ModelBinding;

internal sealed class NoOpBinder : IModelBinder
{
    public static readonly IModelBinder Instance = new NoOpBinder();

    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        return Task.CompletedTask;
    }
}
