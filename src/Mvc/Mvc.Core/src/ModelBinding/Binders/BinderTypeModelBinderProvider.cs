// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using System;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders
{
    /// <summary>
    /// An <see cref="IModelBinderProvider"/> for models which specify an <see cref="IModelBinder"/>
    /// using <see cref="BindingInfo.BinderType"/>.
    /// </summary>
    public class BinderTypeModelBinderProvider : IModelBinderProvider
    {
        /// <inheritdoc />
        public IModelBinder? GetBinder(ModelBinderProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.BindingInfo.BinderType is Type binderType)
            {
                return new BinderTypeModelBinder(binderType);
            }

            return null;
        }
    }
}
