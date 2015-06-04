// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.OptionsModel;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNet.Mvc
{
    public class JsonMvcFormatterMappingOptionsSetup : ConfigureOptions<MvcFormatterMappingOptions>
    {
        public JsonMvcFormatterMappingOptionsSetup()
            : base(ConfigureMvc)
        {
            Order = DefaultOrder.DefaultFrameworkSortOrder + 10;
        }

        public static void ConfigureMvc(MvcFormatterMappingOptions options)
        {
            options.FormatterMappings.SetMediaTypeMappingForFormat("json", MediaTypeHeaderValue.Parse("application/json"));
        }
    }
}
