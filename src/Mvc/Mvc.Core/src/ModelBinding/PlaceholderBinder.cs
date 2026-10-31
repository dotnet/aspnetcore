// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Diagnostics;

namespace Microsoft.AspNetCore.Mvc.ModelBinding;

// Used as a placeholder to break cycles while building a tree of model binders in ModelBinderFactory.
//
// When a cycle is detected by a call to Create(...), we create an instance of this class and return it
// to break the cycle. Later when the 'real' binder is created we set Inner to point to that.
internal sealed class PlaceholderBinder : IModelBinder
{
    public IModelBinder? Inner { get; set; }

    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        Debug.Assert(Inner is not null, "Inner must be resolved before BindModelAsync can be called.");

        return Inner.BindModelAsync(bindingContext);
    }
}
