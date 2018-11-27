// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Localization;

namespace Microsoft.AspNetCore.Mvc.DataAnnotations
{
    /// <summary>
    /// Provides programmatic configuration for DataAnnotations localization in the MVC framework.
    /// </summary>
    public class MvcDataAnnotationsLocalizationOptions
    {
        /// <summary>
        /// The delegate to invoke for creating <see cref="IStringLocalizer"/>.
        /// </summary>
        public Func<Type, IStringLocalizerFactory, IStringLocalizer> DataAnnotationLocalizerProvider;
    }
}
