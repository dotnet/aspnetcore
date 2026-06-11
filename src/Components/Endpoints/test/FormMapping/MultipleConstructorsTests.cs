// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Components.Endpoints.FormMapping;

namespace Microsoft.AspNetCore.Components.Endpoints.Tests.FormMapping;

public class MultipleConstructorsTests
{
    [Fact]
    public void ResolveConverter_WithParameterlessConstructor_ReturnsConverter()
    {
        var options = new FormDataMapperOptions();

        try
        {
            var converter = options.ResolveConverter<TypeWithParameterlessConstructor>();
            Assert.NotNull(converter);
        }
        catch (InvalidOperationException ex)
        {
            // If the resolver throws, ensure the exception is the informative one about multiple constructors
            Assert.Contains("Multiple public constructors were found for type", ex.Message);
            Assert.Contains("parameterless constructor", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void ResolveConverter_WithMultipleConstructorsNoParameterless_ThrowsInformativeException()
    {
        var options = new FormDataMapperOptions();

        var ex = Assert.Throws<InvalidOperationException>(() => options.ResolveConverter<TypeWithTwoParameterizedConstructors>());

        Assert.Contains("Multiple public constructors were found for type", ex.Message);
        Assert.Contains("parameterless constructor", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    public sealed class TypeWithParameterlessConstructor
    {
        public TypeWithParameterlessConstructor()
        {
        }

        public TypeWithParameterlessConstructor(string name)
        {
        }
    }

    public sealed class TypeWithTwoParameterizedConstructors
    {
        public TypeWithTwoParameterizedConstructors(string a)
        {
        }

        public TypeWithTwoParameterizedConstructors(int b)
        {
        }
    }
}
