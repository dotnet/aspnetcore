// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Newtonsoft.Json;

namespace FormatterWebSite
{
    public class InfinitelyRecursiveModel
    {
        [JsonConverter(typeof(StringIdentifierConverter))]
        public RecursiveIdentifier Id { get; set; }

        private class StringIdentifierConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType) => objectType == typeof(RecursiveIdentifier);

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                return new RecursiveIdentifier(reader.Value.ToString());
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                throw new NotImplementedException();
            }
        }
    }
}
