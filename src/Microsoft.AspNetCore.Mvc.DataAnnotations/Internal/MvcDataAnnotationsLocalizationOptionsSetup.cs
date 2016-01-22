// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.DataAnnotations.Internal
{
    /// <summary>
    /// Sets up default options for <see cref="MvcDataAnnotationsLocalizationOptions"/>.
    /// </summary>
    public class MvcDataAnnotationsLocalizationOptionsSetup : ConfigureOptions<MvcDataAnnotationsLocalizationOptions>
    {
        public MvcDataAnnotationsLocalizationOptionsSetup()
            : base(ConfigureMvc)
        {
        }

        public static void ConfigureMvc(MvcDataAnnotationsLocalizationOptions options)
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
