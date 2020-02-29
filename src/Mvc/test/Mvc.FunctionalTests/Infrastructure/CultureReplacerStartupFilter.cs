// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    /// <summary>
    /// Inserts the <see cref="CultureReplacerMiddleware"/> at the beginning of the pipeline.
    /// </summary>
    public class CultureReplacerStartupFilter : IStartupFilter
    {
        /// <inheritdoc />
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return AddCulture;

            void AddCulture(IApplicationBuilder builder)
            {
                builder.UseMiddleware<CultureReplacerMiddleware>();
                next(builder);
            }
        }
    }
}
