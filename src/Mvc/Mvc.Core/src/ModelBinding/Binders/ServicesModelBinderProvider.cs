// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using System;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders
{
    /// <summary>
    /// An <see cref="IModelBinderProvider"/> for binding from the <see cref="IServiceProvider"/>.
    /// </summary>
    public class ServicesModelBinderProvider : IModelBinderProvider
    {
        // ServicesModelBinder does not have any state. Re-use the same instance for binding.

        private readonly ServicesModelBinder _modelBinder = new ServicesModelBinder();

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
                return _modelBinder;
            }

            return null;
        }
    }
}
