// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.OptionsModel;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNet.Mvc.Formatters.Json.Internal
{
    public class MvcJsonMvcOptionsSetup : ConfigureOptions<MvcOptions>
    {
        public MvcJsonMvcOptionsSetup(IOptions<MvcJsonOptions> jsonOptions)
            : base((_) => ConfigureMvc(_, jsonOptions.Value.SerializerSettings))
        {
        }

        public static void ConfigureMvc(MvcOptions options, JsonSerializerSettings serializerSettings)
        {
            options.OutputFormatters.Add(new JsonOutputFormatter(serializerSettings));

            options.InputFormatters.Add(new JsonInputFormatter(serializerSettings));
            options.InputFormatters.Add(new JsonPatchInputFormatter(serializerSettings));

            options.FormatterMappings.SetMediaTypeMappingForFormat("json", MediaTypeHeaderValue.Parse("application/json"));

            options.ValidationExcludeFilters.Add(typeof(JToken));
        }
    }
}
