// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    // Used as a placeholder to break cycles while building a tree of model binders in ModelBinderFactory.
    //
    // When a cycle is detected by a call to Create(...), we create an instance of this class and return it
    // to break the cycle. Later when the 'real' binder is created we set Inner to point to that.
    internal class PlaceholderBinder : IModelBinder
    {
        public IModelBinder Inner { get; set; }

        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            return Inner.BindModelAsync(bindingContext);
        }
    }
}
