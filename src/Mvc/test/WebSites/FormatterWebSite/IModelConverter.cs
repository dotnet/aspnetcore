// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using FormatterWebSite.Models;
using Newtonsoft.Json;

namespace FormatterWebSite;

public class IModelConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(IModel);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        return new DerivedModel
        {
            DerivedProperty = reader.Value.ToString(),
        };
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}
