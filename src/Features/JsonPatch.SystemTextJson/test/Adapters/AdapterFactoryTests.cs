// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.JsonPatch.SystemTextJson.Adapters;
using Microsoft.AspNetCore.JsonPatch.SystemTextJson.Internal;
using Xunit;

namespace Microsoft.AspNetCore.JsonPatch.SystemTextJson.Test.Adapters;

public class AdapterFactoryTests
{
    [Fact]
    public void GetListAdapterForListTargets()
    {
        // Arrange
        AdapterFactory factory = new AdapterFactory();

        //Act:
        IAdapter adapter = factory.Create(new List<string>());

        // Assert
        Assert.Equal(typeof(ListAdapter), adapter.GetType());
    }

    [Fact]
    public void GetDictionaryAdapterForDictionaryObjects()
    {
        // Arrange
        AdapterFactory factory = new AdapterFactory();

        //Act:
        IAdapter adapter = factory.Create(new Dictionary<string, string>());

        // Assert
        Assert.Equal(typeof(DictionaryAdapter<string, string>), adapter.GetType());
    }

    private class PocoModel
    { }

    [Fact]
    public void GetPocoAdapterForGenericObjects()
    {
        // Arrange
        AdapterFactory factory = new AdapterFactory();

        //Act:
        IAdapter adapter = factory.Create(new PocoModel());

        // Assert
        Assert.Equal(typeof(PocoAdapter), adapter.GetType());
    }
}
