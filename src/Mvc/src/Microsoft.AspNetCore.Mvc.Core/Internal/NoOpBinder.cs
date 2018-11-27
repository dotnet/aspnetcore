// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    public class NoOpBinder : IModelBinder
    {
        public static readonly IModelBinder Instance = new NoOpBinder();

        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            return Task.CompletedTask;
        }
    }
}
