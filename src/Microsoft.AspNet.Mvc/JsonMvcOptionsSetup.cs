// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.OptionsModel;
using Newtonsoft.Json;

namespace Microsoft.AspNet.Mvc
{
    public class JsonMvcOptionsSetup : ConfigureOptions<MvcOptions>
    {
        public JsonMvcOptionsSetup(IOptions<MvcJsonOptions> jsonOptions)
            : base((_) => ConfigureMvc(_, jsonOptions.Options.SerializerSettings))
        {
            Order = DefaultOrder.DefaultFrameworkSortOrder + 10;
        }

        public static void ConfigureMvc(MvcOptions options, JsonSerializerSettings serializerSettings)
        {
            options.OutputFormatters.Add(new JsonOutputFormatter(serializerSettings));

            options.InputFormatters.Add(new JsonInputFormatter(serializerSettings));
            options.InputFormatters.Add(new JsonPatchInputFormatter(serializerSettings));
        }
    }
}
