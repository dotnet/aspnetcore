// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using Microsoft.AspNetCore.JsonPatch;
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
    public class MvcJsonMvcOptionsSetup : IConfigureOptions<MvcOptions>
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly JsonSerializerSettings _jsonSerializerSettings;
        private readonly ArrayPool<char> _charPool;
        private readonly ObjectPoolProvider _objectPoolProvider;

        public MvcJsonMvcOptionsSetup(
            ILoggerFactory loggerFactory,
            IOptions<MvcJsonOptions> jsonOptions,
            ArrayPool<char> charPool,
            ObjectPoolProvider objectPoolProvider)
        {
            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            if (jsonOptions == null)
            {
                throw new ArgumentNullException(nameof(jsonOptions));
            }

            if (charPool == null)
            {
                throw new ArgumentNullException(nameof(charPool));
            }

            if (objectPoolProvider == null)
            {
                throw new ArgumentNullException(nameof(objectPoolProvider));
            }

            _loggerFactory = loggerFactory;
            _jsonSerializerSettings = jsonOptions.Value.SerializerSettings;
            _charPool = charPool;
            _objectPoolProvider = objectPoolProvider;
        }

        public void Configure(MvcOptions options)
        {
            options.OutputFormatters.Add(new JsonOutputFormatter(_jsonSerializerSettings, _charPool));

            // Register JsonPatchInputFormatter before JsonInputFormatter, otherwise
            // JsonInputFormatter would consume "application/json-patch+json" requests
            // before JsonPatchInputFormatter gets to see them.
            var jsonInputPatchLogger = _loggerFactory.CreateLogger<JsonPatchInputFormatter>();
            options.InputFormatters.Add(new JsonPatchInputFormatter(
                jsonInputPatchLogger,
                _jsonSerializerSettings,
                _charPool,
                _objectPoolProvider,
                options));

            var jsonInputLogger = _loggerFactory.CreateLogger<JsonInputFormatter>();
            options.InputFormatters.Add(new JsonInputFormatter(
                jsonInputLogger,
                _jsonSerializerSettings,
                _charPool,
                _objectPoolProvider,
                options));

            options.FormatterMappings.SetMediaTypeMappingForFormat("json", MediaTypeHeaderValue.Parse("application/json"));

            options.ModelMetadataDetailsProviders.Add(new SuppressChildValidationMetadataProvider(typeof(IJsonPatchDocument)));
            options.ModelMetadataDetailsProviders.Add(new SuppressChildValidationMetadataProvider(typeof(JToken)));
        }
    }
}
