// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Buffers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNetCore.Mvc.Formatters.Json.Internal
{
    /// <summary>
    /// Sets up JSON formatter options for <see cref="MvcOptions"/>.
    /// </summary>
    public class MvcJsonMvcOptionsSetup : ConfigureOptions<MvcOptions>
    {
        /// <summary>
        /// Intiailizes a new instance of <see cref="MvcJsonMvcOptionsSetup"/>.
        /// </summary>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
        /// <param name="jsonOptions"></param>
        /// <param name="charPool"></param>
        /// <param name="objectPoolProvider"></param>
        public MvcJsonMvcOptionsSetup(
            ILoggerFactory loggerFactory,
            IOptions<MvcJsonOptions> jsonOptions,
            ArrayPool<char> charPool,
            ObjectPoolProvider objectPoolProvider)
            : base((options) => ConfigureMvc(
                options,
                jsonOptions.Value.SerializerSettings,
                loggerFactory,
                charPool,
                objectPoolProvider))
        {
        }

        public static void ConfigureMvc(
            MvcOptions options,
            JsonSerializerSettings serializerSettings,
            ILoggerFactory loggerFactory,
            ArrayPool<char> charPool,
            ObjectPoolProvider objectPoolProvider)
        {

            options.OutputFormatters.Add(new JsonOutputFormatter(serializerSettings, charPool));

            var jsonInputLogger = loggerFactory.CreateLogger<JsonInputFormatter>();
            options.InputFormatters.Add(new JsonInputFormatter(
                jsonInputLogger,
                serializerSettings,
                charPool,
                objectPoolProvider));

            var jsonInputPatchLogger = loggerFactory.CreateLogger<JsonPatchInputFormatter>();
            options.InputFormatters.Add(new JsonPatchInputFormatter(
                jsonInputPatchLogger,
                serializerSettings,
                charPool,
                objectPoolProvider));

            options.FormatterMappings.SetMediaTypeMappingForFormat("json", MediaTypeHeaderValue.Parse("application/json"));

            options.ModelMetadataDetailsProviders.Add(new SuppressChildValidationMetadataProvider(typeof(JToken)));
        }
    }
}
