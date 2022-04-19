// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Microsoft.AspNetCore.JsonPatch;

public abstract class Shape
{
    public string ShapeProperty { get; set; }
}

public class Circle : Shape
{
    public string CircleProperty { get; set; }
}

public class Rectangle : Shape
{
    public string RectangleProperty { get; set; }
}

public class Square : Shape
{
    public Rectangle Rectangle { get; set; }
}

public class Canvas
{
    public IList<Shape> Items { get; set; }
}

public class RectangleContractResolver : DefaultContractResolver
{
    protected override JsonConverter ResolveContractConverter(Type objectType)
    {
        if (objectType == typeof(Rectangle))
        {
            return new RectangleJsonConverter();
        }

        return base.ResolveContractConverter(objectType);
    }
}

public class RectangleJsonConverter : CustomCreationConverter<Rectangle>
{
    public override bool CanRead => true;

    public override Rectangle Create(Type objectType)
    {
        throw new NotImplementedException();
    }

    public override object ReadJson(
        JsonReader reader,
        Type objectType,
        object existingValue,
        JsonSerializer serializer)
    {
        return new Rectangle()
        {
            RectangleProperty = reader.Value.ToString()
        };
    }
}
