// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNet.JsonPatch.Exceptions;
using Microsoft.AspNet.JsonPatch.Operations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Microsoft.AspNet.JsonPatch.Converters
{
    public class TypedJsonPatchDocumentConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return true;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            try
            {
                if (reader.TokenType == JsonToken.Null)
                {
                    return null;
                }

                var genericType = objectType.GetGenericArguments()[0];

                // load jObject
                var jObject = JArray.Load(reader);

                // Create target object for Json => list of operations, typed to genericType
                var genericOperation = typeof(Operation<>);
                var concreteOperationType = genericOperation.MakeGenericType(genericType);

                var genericList = typeof(List<>);
                var concreteList = genericList.MakeGenericType(concreteOperationType);

                var targetOperations = Activator.CreateInstance(concreteList);

                //Create a new reader for this jObject, and set all properties to match the original reader.
                JsonReader jObjectReader = jObject.CreateReader();
                jObjectReader.Culture = reader.Culture;
                jObjectReader.DateParseHandling = reader.DateParseHandling;
                jObjectReader.DateTimeZoneHandling = reader.DateTimeZoneHandling;
                jObjectReader.FloatParseHandling = reader.FloatParseHandling;

                // Populate the object properties
                serializer.Populate(jObjectReader, targetOperations);

                // container target: the typed JsonPatchDocument.
                var container = Activator.CreateInstance(objectType, targetOperations, new DefaultContractResolver());

                return container;
            }
            catch (Exception ex)
            {
                throw new JsonPatchException("The JsonPatchDocument was malformed and could not be parsed.", ex);
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