// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Newtonsoft.Json;

namespace Microsoft.HttpRepl.OpenApi
{
    public class EitherConverter<TOption1, TOption2> : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(TOption1).IsAssignableFrom(objectType) || typeof(TOption2).IsAssignableFrom(objectType) || typeof(EitherConverter<TOption1, TOption2>) == objectType;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            try
            {
                TOption1 option1 = serializer.Deserialize<TOption1>(reader);
                return new Either<TOption1, TOption2>(option1);
            }
            catch
            {
                TOption2 option2 = serializer.Deserialize<TOption2>(reader);
                return new Either<TOption1, TOption2>(option2);
            }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
