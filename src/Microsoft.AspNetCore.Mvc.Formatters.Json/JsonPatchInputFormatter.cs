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
    /// <summary>
    /// A <see cref="TextInputFormatter"/> for JSON Patch (application/json-patch+json) content.
    /// </summary>
    public class JsonPatchInputFormatter : JsonInputFormatter
    {
        /// <summary>
        /// Initializes a new <see cref="JsonPatchInputFormatter"/> instance.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/>.</param>
        /// <param name="serializerSettings">
        /// The <see cref="JsonSerializerSettings"/>. Should be either the application-wide settings
        /// (<see cref="MvcJsonOptions.SerializerSettings"/>) or an instance
        /// <see cref="JsonSerializerSettingsProvider.CreateSerializerSettings"/> initially returned.
        /// </param>
        /// <param name="charPool">The <see cref="ArrayPool{Char}"/>.</param>
        /// <param name="objectPoolProvider">The <see cref="ObjectPoolProvider"/>.</param>
        [Obsolete("This constructor is obsolete and will be removed in a future version.")]
        public JsonPatchInputFormatter(
            ILogger logger,
            JsonSerializerSettings serializerSettings,
            ArrayPool<char> charPool,
            ObjectPoolProvider objectPoolProvider)
            : this(logger, serializerSettings, charPool, objectPoolProvider, suppressInputFormatterBuffering: false)
        {
        }

        /// <summary>
        /// Initializes a new <see cref="JsonPatchInputFormatter"/> instance.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/>.</param>
        /// <param name="serializerSettings">
        /// The <see cref="JsonSerializerSettings"/>. Should be either the application-wide settings
        /// (<see cref="MvcJsonOptions.SerializerSettings"/>) or an instance
        /// <see cref="JsonSerializerSettingsProvider.CreateSerializerSettings"/> initially returned.
        /// </param>
        /// <param name="charPool">The <see cref="ArrayPool{Char}"/>.</param>
        /// <param name="objectPoolProvider">The <see cref="ObjectPoolProvider"/>.</param>
        /// <param name="suppressInputFormatterBuffering">Flag to buffer entire request body before deserializing it.</param>
        [Obsolete("This constructor is obsolete and will be removed in a future version.")]
        public JsonPatchInputFormatter(
            ILogger logger,
            JsonSerializerSettings serializerSettings,
            ArrayPool<char> charPool,
            ObjectPoolProvider objectPoolProvider,
            bool suppressInputFormatterBuffering)
            : this(logger, serializerSettings, charPool, objectPoolProvider, suppressInputFormatterBuffering, suppressJsonDeserializationExceptionMessages: false)
        {
        }

        /// <summary>
        /// Initializes a new <see cref="JsonPatchInputFormatter"/> instance.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/>.</param>
        /// <param name="serializerSettings">
        /// The <see cref="JsonSerializerSettings"/>. Should be either the application-wide settings
        /// (<see cref="MvcJsonOptions.SerializerSettings"/>) or an instance
        /// <see cref="JsonSerializerSettingsProvider.CreateSerializerSettings"/> initially returned.
        /// </param>
        /// <param name="charPool">The <see cref="ArrayPool{Char}"/>.</param>
        /// <param name="objectPoolProvider">The <see cref="ObjectPoolProvider"/>.</param>
        /// <param name="suppressInputFormatterBuffering">Flag to buffer entire request body before deserializing it.</param>
        /// <param name="suppressJsonDeserializationExceptionMessages">If <see langword="true"/>, JSON deserialization exception messages will replaced by a generic message in model state.</param>
        [Obsolete("This constructor is obsolete and will be removed in a future version.")]
        public JsonPatchInputFormatter(
            ILogger logger,
            JsonSerializerSettings serializerSettings,
            ArrayPool<char> charPool,
            ObjectPoolProvider objectPoolProvider,
            bool suppressInputFormatterBuffering,
            bool suppressJsonDeserializationExceptionMessages)
            : base(logger, serializerSettings, charPool, objectPoolProvider, suppressInputFormatterBuffering, suppressJsonDeserializationExceptionMessages)
        {
            // Clear all values and only include json-patch+json value.
            SupportedMediaTypes.Clear();

            SupportedMediaTypes.Add(MediaTypeHeaderValues.ApplicationJsonPatch);
        }

        /// <summary>
        /// Initializes a new <see cref="JsonPatchInputFormatter"/> instance.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/>.</param>
        /// <param name="serializerSettings">
        /// The <see cref="JsonSerializerSettings"/>. Should be either the application-wide settings
        /// (<see cref="MvcJsonOptions.SerializerSettings"/>) or an instance
        /// <see cref="JsonSerializerSettingsProvider.CreateSerializerSettings"/> initially returned.
        /// </param>
        /// <param name="charPool">The <see cref="ArrayPool{Char}"/>.</param>
        /// <param name="objectPoolProvider">The <see cref="ObjectPoolProvider"/>.</param>
        /// <param name="options">The <see cref="MvcOptions"/>.</param>
        public JsonPatchInputFormatter(
            ILogger logger,
            JsonSerializerSettings serializerSettings,
            ArrayPool<char> charPool,
            ObjectPoolProvider objectPoolProvider,
            MvcOptions options)
            : base(logger, serializerSettings, charPool, objectPoolProvider, options)
        {
            // Clear all values and only include json-patch+json value.
            SupportedMediaTypes.Clear();

            SupportedMediaTypes.Add(MediaTypeHeaderValues.ApplicationJsonPatch);
        }
        
        /// <inheritdoc />
        public override InputFormatterExceptionModelStatePolicy ExceptionPolicy
        {
            get
            {
                if (GetType() == typeof(JsonPatchInputFormatter))
                {
                    return InputFormatterExceptionModelStatePolicy.MalformedInputExceptions;
                }
                return InputFormatterExceptionModelStatePolicy.AllExceptions;
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