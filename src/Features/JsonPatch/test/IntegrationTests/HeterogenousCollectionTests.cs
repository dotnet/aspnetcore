// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Xunit;

namespace Microsoft.AspNetCore.JsonPatch.IntegrationTests;

public class HeterogenousCollectionTests
{
    [Fact]
    public void AddItemToList()
    {
        // Arrange
        var targetObject = new Canvas()
        {
            Items = new List<Shape>()
        };

        var circleJObject = JObject.Parse(@"{
              Type: 'Circle',
              ShapeProperty: 'Shape property',
              CircleProperty: 'Circle property'
            }");

        var patchDocument = new JsonPatchDocument
        {
            ContractResolver = new CanvasContractResolver()
        };

        patchDocument.Add("/Items/-", circleJObject);

        // Act
        patchDocument.ApplyTo(targetObject);

        // Assert
        var circle = targetObject.Items[0] as Circle;
        Assert.NotNull(circle);
        Assert.Equal("Shape property", circle.ShapeProperty);
        Assert.Equal("Circle property", circle.CircleProperty);
    }
}

public class CanvasContractResolver : DefaultContractResolver
{
    protected override JsonConverter ResolveContractConverter(Type objectType)
    {
        if (objectType == typeof(Shape))
        {
            return new ShapeJsonConverter();
        }

        return base.ResolveContractConverter(objectType);
    }
}

public class ShapeJsonConverter : CustomCreationConverter<Shape>
{
    private const string TypeProperty = "Type";

    public override bool CanRead => true;

    public override Shape Create(Type objectType)
    {
        throw new NotImplementedException();
    }

    public override object ReadJson(
        JsonReader reader,
        Type objectType,
        object existingValue,
        JsonSerializer serializer)
    {
        var jObject = JObject.Load(reader);

        var target = CreateShape(jObject);
        serializer.Populate(jObject.CreateReader(), target);

        return target;
    }

    private Shape CreateShape(JObject jObject)
    {
        var typeProperty = jObject.GetValue(TypeProperty).ToString();

        switch (typeProperty)
        {
            case "Circle":
                return new Circle();

            case "Rectangle":
                return new Rectangle();
        }

        throw new NotSupportedException();
    }
}
