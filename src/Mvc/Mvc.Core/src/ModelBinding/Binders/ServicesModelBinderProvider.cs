// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
