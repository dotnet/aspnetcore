
// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Html;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.Rendering
{
    internal class DefaultJsonHelper : IJsonHelper
    {
        /// <inheritdoc />
        public IHtmlContent Serialize(object value)
        {
            throw new InvalidOperationException(Core.Resources.FormatReferenceToNewtonsoftJsonRequired(
               $"{nameof(IJsonHelper)}.{nameof(Serialize)}",
               "Microsoft.AspNetCore.Mvc.NewtonsoftJson",
               nameof(IMvcBuilder),
               "AddNewtonsoftJson",
               "ConfigureServices(...)"));
        }
    }
}
