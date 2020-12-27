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
    /// An <see cref="IModelBinderProvider"/> for binding <see cref="DateTime" /> and nullable <see cref="DateTime"/> models.
    /// </summary>
    public class DateTimeModelBinderProvider : IModelBinderProvider
    {
        internal static readonly DateTimeStyles SupportedStyles = DateTimeStyles.AdjustToUniversal | DateTimeStyles.AllowWhiteSpaces;

        /// <inheritdoc />
        public IModelBinder? GetBinder(ModelBinderProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var modelType = context.Metadata.UnderlyingOrModelType;
            if (modelType == typeof(DateTime))
            {
                var loggerFactory = context.Services.GetRequiredService<ILoggerFactory>();
                return new DateTimeModelBinder(SupportedStyles, loggerFactory);
            }

            return null;
        }
    }
}
