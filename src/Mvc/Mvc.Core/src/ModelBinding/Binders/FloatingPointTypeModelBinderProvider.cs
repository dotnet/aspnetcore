// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using System;
using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders
{
    /// <summary>
    /// An <see cref="IModelBinderProvider"/> for binding <see cref="decimal"/>, <see cref="double"/>,
    /// <see cref="float"/>, and their <see cref="Nullable{T}"/> wrappers.
    /// </summary>
    public class FloatingPointTypeModelBinderProvider : IModelBinderProvider
    {
        // SimpleTypeModelBinder uses DecimalConverter and similar. Those TypeConverters default to NumberStyles.Float.
        // Internal for testing.
        internal static readonly NumberStyles SupportedStyles = NumberStyles.Float | NumberStyles.AllowThousands;

        /// <inheritdoc />
        public IModelBinder? GetBinder(ModelBinderProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var modelType = context.Metadata.UnderlyingOrModelType;
            var loggerFactory = context.Services.GetRequiredService<ILoggerFactory>();
            if (modelType == typeof(decimal))
            {
                return new DecimalModelBinder(SupportedStyles, loggerFactory);
            }

            if (modelType == typeof(double))
            {
                return new DoubleModelBinder(SupportedStyles, loggerFactory);
            }

            if (modelType == typeof(float))
            {
                return new FloatModelBinder(SupportedStyles, loggerFactory);
            }

            return null;
        }
    }
}
