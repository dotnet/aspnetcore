// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.JsonPatch.Exceptions;
using Microsoft.AspNetCore.JsonPatch.Operations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Microsoft.AspNetCore.JsonPatch.Converters
{
    public class JsonPatchDocumentConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return true;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, 
            JsonSerializer serializer)
        {
            if (objectType != typeof(JsonPatchDocument))
            {
                throw new ArgumentException(Resources.FormatParameterMustMatchType("objectType", "JsonPatchDocument"), "objectType");
            }

            try
            {
                if (reader.TokenType == JsonToken.Null)
                {
                    return null;
                }
                
                // load jObject
                var jObject = JArray.Load(reader);

                // Create target object for Json => list of operations
                var targetOperations = new List<Operation>();

                // Create a new reader for this jObject, and set all properties 
                // to match the original reader.
                var jObjectReader = jObject.CreateReader();
                jObjectReader.Culture = reader.Culture;
                jObjectReader.DateParseHandling = reader.DateParseHandling;
                jObjectReader.DateTimeZoneHandling = reader.DateTimeZoneHandling;
                jObjectReader.FloatParseHandling = reader.FloatParseHandling;

                // Populate the object properties
                serializer.Populate(jObjectReader, targetOperations);

                // container target: the JsonPatchDocument. 
                var container = new JsonPatchDocument(targetOperations, new DefaultContractResolver());

                return container;
            }
            catch (Exception ex)
            {
                throw new JsonSerializationException(Resources.InvalidJsonPatchDocument, ex);
            }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value is IJsonPatchDocument)
            {
                var jsonPatchDoc = (IJsonPatchDocument)value;
                var lst = jsonPatchDoc.GetOperations();

                // write out the operations, no envelope
                serializer.Serialize(writer, lst);
            }
        }
    }
}
