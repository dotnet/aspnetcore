// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Dynamic;
using Microsoft.AspNetCore.JsonPatch.Exceptions;
using Microsoft.AspNetCore.JsonPatch.Operations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.AspNetCore.JsonPatch;

public class JsonPatchDocumentJObjectTest
{
    [Fact]
    public void ApplyTo_Array_Add()
    {
        // Arrange
        var model = new ObjectWithJObject { CustomData = JObject.FromObject(new { Emails = new[] { "foo@bar.com" } }) };
        var patch = new JsonPatchDocument<ObjectWithJObject>();

        patch.Operations.Add(new Operation<ObjectWithJObject>("add", "/CustomData/Emails/-", null, "foo@baz.com"));

        // Act
        patch.ApplyTo(model);

        // Assert
        Assert.Equal("foo@baz.com", model.CustomData["Emails"][1].Value<string>());
    }

    [Fact]
    public void ApplyTo_Model_Test1()
    {
        // Arrange
        var model = new ObjectWithJObject { CustomData = JObject.FromObject(new { Email = "foo@bar.com", Name = "Bar" }) };
        var patch = new JsonPatchDocument<ObjectWithJObject>();

        patch.Operations.Add(new Operation<ObjectWithJObject>("test", "/CustomData/Email", null, "foo@baz.com"));
        patch.Operations.Add(new Operation<ObjectWithJObject>("add", "/CustomData/Name", null, "Bar Baz"));

        // Act & Assert
        Assert.Throws<JsonPatchException>(() => patch.ApplyTo(model));
    }

    [Fact]
    public void ApplyTo_Model_Test2()
    {
        // Arrange
        var model = new ObjectWithJObject { CustomData = JObject.FromObject(new { Email = "foo@bar.com", Name = "Bar" }) };
        var patch = new JsonPatchDocument<ObjectWithJObject>();

        patch.Operations.Add(new Operation<ObjectWithJObject>("test", "/CustomData/Email", null, "foo@bar.com"));
        patch.Operations.Add(new Operation<ObjectWithJObject>("add", "/CustomData/Name", null, "Bar Baz"));

        // Act
        patch.ApplyTo(model);

        // Assert
        Assert.Equal("Bar Baz", model.CustomData["Name"].Value<string>());
    }

    [Fact]
    public void ApplyTo_Model_Copy()
    {
        // Arrange
        var model = new ObjectWithJObject { CustomData = JObject.FromObject(new { Email = "foo@bar.com" }) };
        var patch = new JsonPatchDocument<ObjectWithJObject>();

        patch.Operations.Add(new Operation<ObjectWithJObject>("copy", "/CustomData/UserName", "/CustomData/Email"));

        // Act
        patch.ApplyTo(model);

        // Assert
        Assert.Equal("foo@bar.com", model.CustomData["UserName"].Value<string>());
    }

    [Fact]
    public void ApplyTo_Model_Remove()
    {
        // Arrange
        var model = new ObjectWithJObject { CustomData = JObject.FromObject(new { FirstName = "Foo", LastName = "Bar" }) };
        var patch = new JsonPatchDocument<ObjectWithJObject>();

        patch.Operations.Add(new Operation<ObjectWithJObject>("remove", "/CustomData/LastName", null));

        // Act
        patch.ApplyTo(model);

        // Assert
        Assert.False(model.CustomData.ContainsKey("LastName"));
    }

    [Fact]
    public void ApplyTo_Model_Move()
    {
        // Arrange
        var model = new ObjectWithJObject { CustomData = JObject.FromObject(new { FirstName = "Bar" }) };
        var patch = new JsonPatchDocument<ObjectWithJObject>();

        patch.Operations.Add(new Operation<ObjectWithJObject>("move", "/CustomData/LastName", "/CustomData/FirstName"));

        // Act
        patch.ApplyTo(model);

        // Assert
        Assert.False(model.CustomData.ContainsKey("FirstName"));
        Assert.Equal("Bar", model.CustomData["LastName"].Value<string>());
    }

    [Fact]
    public void ApplyTo_Model_Add()
    {
        // Arrange
        var model = new ObjectWithJObject();
        var patch = new JsonPatchDocument<ObjectWithJObject>();

        patch.Operations.Add(new Operation<ObjectWithJObject>("add", "/CustomData/Name", null, "Foo"));

        // Act
        patch.ApplyTo(model);

        // Assert
        Assert.Equal("Foo", model.CustomData["Name"].Value<string>());
    }

    [Fact]
    public void ApplyTo_Model_Add_Null()
    {
        // Arrange
        var model = new ObjectWithJObject();
        var patch = new JsonPatchDocument<ObjectWithJObject>();

        patch.Operations.Add(new Operation<ObjectWithJObject>("add", "/CustomData/Name", null, null));

        // Act
        patch.ApplyTo(model);

        // Assert
        Assert.Equal(JTokenType.Null, model.CustomData["Name"].Type);
    }

    [Fact]
    public void ApplyTo_Model_Replace()
    {
        // Arrange
        var model = new ObjectWithJObject { CustomData = JObject.FromObject(new { Email = "foo@bar.com", Name = "Bar" }) };
        var patch = new JsonPatchDocument<ObjectWithJObject>();

        patch.Operations.Add(new Operation<ObjectWithJObject>("replace", "/CustomData/Email", null, "foo@baz.com"));

        // Act
        patch.ApplyTo(model);

        // Assert
        Assert.Equal("foo@baz.com", model.CustomData["Email"].Value<string>());
    }

    [Fact]
    public void ApplyTo_Model_Replace_Null()
    {
        // Arrange
        var model = new ObjectWithJObject { CustomData = JObject.FromObject(new { Email = "foo@bar.com", Name = "Bar" }) };
        var patch = new JsonPatchDocument<ObjectWithJObject>();

        patch.Operations.Add(new Operation<ObjectWithJObject>("replace", "/CustomData/Email", null, null));

        // Act
        patch.ApplyTo(model);

        // Assert
        Assert.Equal(JTokenType.Null, model.CustomData["Email"].Type);
    }
}
