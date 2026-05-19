// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;
using Microsoft.AspNetCore.JsonPatch.Adapters;
using Microsoft.AspNetCore.JsonPatch.Internal;
using Moq;
using Newtonsoft.Json.Serialization;
using Xunit;

namespace Microsoft.AspNetCore.JsonPatch.Test.Adapters;

public class AdapterFactoryTests
{
    [Fact]
    public void GetListAdapterForListTargets()
    {
        // Arrange
        AdapterFactory factory = new AdapterFactory();

        //Act:
        IAdapter adapter = factory.Create(new List<string>(), new DefaultContractResolver());

        // Assert
        Assert.Equal(typeof(ListAdapter), adapter.GetType());
    }

    [Fact]
    public void GetDictionaryAdapterForDictionaryObjects()
    {
        // Arrange
        AdapterFactory factory = new AdapterFactory();

        //Act:
        IAdapter adapter = factory.Create(new Dictionary<string, string>(), new DefaultContractResolver());

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
        IAdapter adapter = factory.Create(new PocoModel(), new DefaultContractResolver());

        // Assert
        Assert.Equal(typeof(PocoAdapter), adapter.GetType());
    }

    [Fact]
    public void GetDynamicAdapterForGenericObjects()
    {
        // Arrange
        AdapterFactory factory = new AdapterFactory();

        //Act:
        IAdapter adapter = factory.Create(new TestDynamicObject(), new DefaultContractResolver());

        // Assert
        Assert.Equal(typeof(DynamicObjectAdapter), adapter.GetType());
    }
}
