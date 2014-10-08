// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Http.Formatting;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Mvc.WebApiCompatShim
{
    public class WebApiCompatShimOptionsSetup : IOptionsAction<MvcOptions>, IOptionsAction<WebApiCompatShimOptions>
    {
        public int Order
        {
            // We want to run after the default MvcOptionsSetup.
            get { return DefaultOrder.DefaultFrameworkSortOrder + 100; }
        }

        public string Name { get; set; }

        public void Invoke(MvcOptions options)
        {
            // Placeholder
        }

        public void Invoke(WebApiCompatShimOptions options)
        {
            // Add the default formatters
            options.Formatters.AddRange(new MediaTypeFormatterCollection());
        }
    }
}
