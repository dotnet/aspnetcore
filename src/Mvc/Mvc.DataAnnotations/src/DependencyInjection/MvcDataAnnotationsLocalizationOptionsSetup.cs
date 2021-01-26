// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.DataAnnotations;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Sets up default options for <see cref="MvcDataAnnotationsLocalizationOptions"/>.
    /// </summary>
    internal class MvcDataAnnotationsLocalizationOptionsSetup : IConfigureOptions<MvcDataAnnotationsLocalizationOptions>
    {
        /// <inheritdoc />
        public void Configure(MvcDataAnnotationsLocalizationOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            options.DataAnnotationLocalizerProvider = (modelType, stringLocalizerFactory) =>
                stringLocalizerFactory.Create(modelType);
        }
    }
}
