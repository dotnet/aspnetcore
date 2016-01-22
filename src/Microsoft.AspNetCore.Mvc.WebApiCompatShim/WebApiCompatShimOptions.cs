// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Net.Http.Formatting;

namespace Microsoft.AspNetCore.Mvc.WebApiCompatShim
{
    public class WebApiCompatShimOptions
    {
        public WebApiCompatShimOptions()
        {
            // Start with an empty collection, our options setup will add the default formatters.
            Formatters = new MediaTypeFormatterCollection(Enumerable.Empty<MediaTypeFormatter>());
        }

        public MediaTypeFormatterCollection Formatters { get; set; }
    }
}