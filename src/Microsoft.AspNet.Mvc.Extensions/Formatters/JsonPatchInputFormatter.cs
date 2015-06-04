// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNet.JsonPatch;
using Microsoft.Framework.Internal;
using Microsoft.Net.Http.Headers;
using Microsoft.AspNet.Mvc.Internal;
using Newtonsoft.Json;

namespace Microsoft.AspNet.Mvc
{
    public class JsonPatchInputFormatter : JsonInputFormatter
    {
        public JsonPatchInputFormatter()
            : this(SerializerSettingsProvider.CreateSerializerSettings())
        {
        }

        public JsonPatchInputFormatter([NotNull] JsonSerializerSettings serializerSettings)
            : base(serializerSettings)
        {
            // Clear all values and only include json-patch+json value.
            SupportedMediaTypes.Clear();

            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/json-patch+json"));
        }

        /// <inheritdoc />
        public async override Task<object> ReadRequestBodyAsync([NotNull] InputFormatterContext context)
        {
            var jsonPatchDocument = (IJsonPatchDocument)(await base.ReadRequestBodyAsync(context));
            if (jsonPatchDocument != null && SerializerSettings.ContractResolver != null)
            {
                jsonPatchDocument.ContractResolver = SerializerSettings.ContractResolver;
            }

            return (object)jsonPatchDocument;
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