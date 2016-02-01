// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc.Formatters.Json.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.Mvc.Formatters
{
    public class JsonPatchInputFormatter : JsonInputFormatter
    {
        public JsonPatchInputFormatter(ILogger logger)
            : this(
                  logger,
                  SerializerSettingsProvider.CreateSerializerSettings(),
                  ArrayPool<char>.Shared,
                  new DefaultObjectPoolProvider())
        {
        }

        public JsonPatchInputFormatter(
            ILogger logger,
            JsonSerializerSettings serializerSettings,
            ArrayPool<char> charPool,
            ObjectPoolProvider objectPoolProvider)
            : base(logger, serializerSettings, charPool, objectPoolProvider)
        {
            // Clear all values and only include json-patch+json value.
            SupportedMediaTypes.Clear();

            SupportedMediaTypes.Add(MediaTypeHeaderValues.ApplicationJsonPatch);
        }

        /// <inheritdoc />
        public async override Task<InputFormatterResult> ReadRequestBodyAsync(
            InputFormatterContext context,
            Encoding encoding)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (encoding == null)
            {
                throw new ArgumentNullException(nameof(encoding));
            }

            var result = await base.ReadRequestBodyAsync(context, encoding);
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
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

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