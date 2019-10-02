// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders
{
    /// <summary>
    /// A <see cref="IModelBinderProvider"/> for types deriving from <see cref="Enum"/>.
    /// </summary>
    public class EnumTypeModelBinderProvider : IModelBinderProvider
    {
        /// <summary>
        /// Initializes a new instance of <see cref="EnumTypeModelBinderProvider"/>.
        /// </summary>
        /// <param name="options">The <see cref="MvcOptions"/>.</param>
        /// <remarks>The <paramref name="options"/> parameter is currently ignored.</remarks>
        public EnumTypeModelBinderProvider(MvcOptions options)
        {
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
                var loggerFactory = context.Services.GetRequiredService<ILoggerFactory>();
                return new EnumTypeModelBinder(
                    suppressBindingUndefinedValueToEnumType: true,
                    context.Metadata.UnderlyingOrModelType,
                    loggerFactory);
            }

            return null;
        }
    }
}
