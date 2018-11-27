// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.RazorPages
{
    internal class RazorPagesOptionsConfigureCompatibilityOptions : ConfigureCompatibilityOptions<RazorPagesOptions>
    {
        public RazorPagesOptionsConfigureCompatibilityOptions(
            ILoggerFactory loggerFactory,
            IOptions<MvcCompatibilityOptions> compatibilityOptions) 
            : base(loggerFactory, compatibilityOptions)
        {
        }

        protected override IReadOnlyDictionary<string, object> DefaultValues
        {
            get
            {
                var values = new Dictionary<string, object>();

                if (Version >= CompatibilityVersion.Version_2_1)
                {
                    values[nameof(RazorPagesOptions.AllowAreas)] = true;
                    values[nameof(RazorPagesOptions.AllowMappingHeadRequestsToGetHandler)] = true;
                }

                return values;
            }
        }
    }
}
