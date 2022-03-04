// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc.NewtonsoftJson;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.Mvc.Formatters
{
    /// <summary>
    /// A <see cref="TextInputFormatter"/> for JSON Patch (application/json-patch+json) content.
    /// </summary>
    public class NewtonsoftJsonPatchInputFormatter : NewtonsoftJsonInputFormatter
    {
        /// <summary>
        /// Initializes a new <see cref="NewtonsoftJsonPatchInputFormatter"/> instance.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/>.</param>
        /// <param name="serializerSettings">
        /// The <see cref="JsonSerializerSettings"/>. Should be either the application-wide settings
        /// (<see cref="MvcNewtonsoftJsonOptions.SerializerSettings"/>) or an instance
        /// <see cref="JsonSerializerSettingsProvider.CreateSerializerSettings"/> initially returned.
        /// </param>
        /// <param name="charPool">The <see cref="ArrayPool{Char}"/>.</param>
        /// <param name="objectPoolProvider">The <see cref="ObjectPoolProvider"/>.</param>
        /// <param name="options">The <see cref="MvcOptions"/>.</param>
        /// <param name="jsonOptions">The <see cref="MvcNewtonsoftJsonOptions"/>.</param>
        public NewtonsoftJsonPatchInputFormatter(
            ILogger logger,
            JsonSerializerSettings serializerSettings,
            ArrayPool<char> charPool,
            ObjectPoolProvider objectPoolProvider,
            MvcOptions options,
            MvcNewtonsoftJsonOptions jsonOptions)
            : base(logger, serializerSettings, charPool, objectPoolProvider, options, jsonOptions)
        {
            // Clear all values and only include json-patch+json value.
            SupportedMediaTypes.Clear();

            SupportedMediaTypes.Add(MediaTypeHeaderValues.ApplicationJsonPatch);
        }

        /// <inheritdoc />
        public override InputFormatterExceptionPolicy ExceptionPolicy
        {
            get
            {
                if (GetType() == typeof(NewtonsoftJsonPatchInputFormatter))
                {
                    return InputFormatterExceptionPolicy.MalformedInputExceptions;
                }
                return InputFormatterExceptionPolicy.AllExceptions;
            }
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
