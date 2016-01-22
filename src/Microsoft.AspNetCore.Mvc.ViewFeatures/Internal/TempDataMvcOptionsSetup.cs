// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Internal
{
    /// <summary>
    /// Sets up default options for <see cref="MvcOptions"/>.
    /// </summary>
    public class TempDataMvcOptionsSetup : ConfigureOptions<MvcOptions>
    {
        public TempDataMvcOptionsSetup()
            : base(ConfigureMvc)
        {
        }

        public static void ConfigureMvc(MvcOptions options)
        {
            options.Filters.Add(new SaveTempDataAttribute());
        }
    }
}
