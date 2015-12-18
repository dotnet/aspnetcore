// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Buffers;
using Microsoft.AspNet.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNet.Mvc.Formatters.Json.Internal
{
    public class MvcJsonMvcOptionsSetup : ConfigureOptions<MvcOptions>
    {
        public MvcJsonMvcOptionsSetup(
            ILoggerFactory loggerFactory,
            IOptions<MvcJsonOptions> jsonOptions,
            ArrayPool<char> charPool)
            : base((options) => ConfigureMvc(options, jsonOptions.Value.SerializerSettings, loggerFactory, charPool))
        {
        }

        public static void ConfigureMvc(
            MvcOptions options,
            JsonSerializerSettings serializerSettings,
            ILoggerFactory loggerFactory,
            ArrayPool<char> charPool)
        {
            var jsonInputLogger = loggerFactory.CreateLogger<JsonInputFormatter>();
            var jsonInputPatchLogger = loggerFactory.CreateLogger<JsonPatchInputFormatter>();

            options.OutputFormatters.Add(new JsonOutputFormatter(serializerSettings, charPool));
            options.InputFormatters.Add(new JsonInputFormatter(jsonInputLogger, serializerSettings, charPool));
            options.InputFormatters.Add(new JsonPatchInputFormatter(jsonInputPatchLogger, serializerSettings, charPool));
            
            options.FormatterMappings.SetMediaTypeMappingForFormat("json", MediaTypeHeaderValue.Parse("application/json"));

            options.ModelMetadataDetailsProviders.Add(new ValidationExcludeFilter(typeof(JToken)));
        }
    }
}
