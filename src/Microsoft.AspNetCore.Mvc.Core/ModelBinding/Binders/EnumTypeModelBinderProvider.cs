// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders
{
    /// <summary>
    /// A <see cref="IModelBinderProvider"/> for types deriving from <see cref="Enum"/>.
    /// </summary>
    public class EnumTypeModelBinderProvider : IModelBinderProvider
    {
        private readonly MvcOptions _options;

        public EnumTypeModelBinderProvider(MvcOptions options)
        {
            _options = options;
        }

        /// <inheritdoc />
        public IModelBinder GetBinder(ModelBinderProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.Metadata.IsEnum)
            {
                return new EnumTypeModelBinder(
                    _options.AllowBindingUndefinedValueToEnumType,
                    context.Metadata.UnderlyingOrModelType);
            }

            return null;
        }
    }
}
