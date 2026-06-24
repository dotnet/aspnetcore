// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Endpoints.FormMapping;

namespace Microsoft.AspNetCore.Components.Endpoints.Tests.FormMapping;

public class MultipleConstructorsTests
{
    [Fact]

    public void ResolveConverter_WithParameterlessConstructor_ReturnsConverter()
    {
        var options = new FormDataMapperOptions();

        var converter = options.ResolveConverter<TypeWithParameterlessConstructor>();

        Assert.NotNull(converter);
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
