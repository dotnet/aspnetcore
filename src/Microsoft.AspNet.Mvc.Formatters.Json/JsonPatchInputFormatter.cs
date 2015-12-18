// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNet.JsonPatch;
using Microsoft.AspNet.Mvc.Internal;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Microsoft.AspNet.Mvc.Formatters
{
    public class JsonPatchInputFormatter : JsonInputFormatter
    {
        public JsonPatchInputFormatter(ILogger logger)
            : this(logger, SerializerSettingsProvider.CreateSerializerSettings(), ArrayPool<char>.Shared)
        {
        }

        public JsonPatchInputFormatter(
            ILogger logger,
            JsonSerializerSettings serializerSettings,
            ArrayPool<char> charPool)
            : base(logger, serializerSettings, charPool)
        {
            // Clear all values and only include json-patch+json value.
            SupportedMediaTypes.Clear();

            SupportedMediaTypes.Add(MediaTypeHeaderValues.ApplicationJsonPatch);
        }

        /// <inheritdoc />
        public async override Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var result = await base.ReadRequestBodyAsync(context);
            if (!result.HasError)
            {
                var jsonPatchDocument = (IJsonPatchDocument)result.Model;
                if (jsonPatchDocument != null && SerializerSettings.ContractResolver != null)
                {
                    jsonPatchDocument.ContractResolver = SerializerSettings.ContractResolver;
                }
            }

            return result;
        }

        /// <inheritdoc />
        public override bool CanRead(InputFormatterContext context)
        {
            var modelTypeInfo = context.ModelType.GetTypeInfo();
            if (!typeof(IJsonPatchDocument).GetTypeInfo().IsAssignableFrom(modelTypeInfo) ||
                !modelTypeInfo.IsGenericType)
            {
                return false;
            }

            return base.CanRead(context);
        }
    }
}