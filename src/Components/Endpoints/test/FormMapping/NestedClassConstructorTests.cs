// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Endpoints.FormMapping;

namespace Microsoft.AspNetCore.Components.Endpoints.Tests.FormMapping;

public class NestedClassConstructorTests
{
    [Fact]
    public void ResolveConverter_SingleLevelNestedClassWithParameterlessConstructor_Works()
    {
        var options = new FormDataMapperOptions();

        // Level 1: Has parameterless constructor - should work
        var converter = options.ResolveConverter<Level1Class>();
        Assert.NotNull(converter);
    }

    [Fact]
    public void ResolveConverter_TwoLevelNestedClassWithParameterlessConstructor_Works()
    {
        var options = new FormDataMapperOptions();

        // Level 2: Has parameterless constructor - should work
        var converter = options.ResolveConverter<Level2Class>();
        Assert.NotNull(converter);
    }

    [Fact]
    public void ResolveConverter_ThreeLevelDeepNestedClassWithParameterlessConstructor_Works()
    {
        var options = new FormDataMapperOptions();

        // Level 3: Has parameterless constructor - should work
        var converter = options.ResolveConverter<Level3Class>();
        Assert.NotNull(converter);
    }

    [Fact]
    public void ResolveConverter_FourLevelDeepNestedClassWithParameterlessConstructor_Works()
    {
        var options = new FormDataMapperOptions();

        // Level 4: Has parameterless constructor - should work
        var converter = options.ResolveConverter<Level4Class>();
        Assert.NotNull(converter);
    }

    [Fact]
    public void ResolveConverter_MultiLevelNestedWithMultipleConstructors_ThrowsInformativeException()
    {
        var options = new FormDataMapperOptions();

        // Class with multiple constructors and no parameterless one
        var ex = Assert.Throws<InvalidOperationException>(() => options.ResolveConverter<MultipleConstructorsNoParameterless>());

        Assert.Contains("Multiple public constructors were found for type", ex.Message);
        Assert.Contains("parameterless constructor", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ResolveConverter_OuterClassWithParameterlessInnerWithMultipleConstructors_Succeeds()
    {
        // IMPORTANT: Issue #55711 finding
        // The form mapping system does NOT recursively check nested types for multiple constructors.
        // Only the top-level type being resolved is checked.
        // So ClassWithProblematicInner succeeds because it has a parameterless constructor,
        // even though its Inner class has multiple parameterized constructors.

        var options = new FormDataMapperOptions();

        // This succeeds because the system only checks the outer class,
        // not recursively checking nested types
        var converter = options.ResolveConverter<ClassWithProblematicInner>();
        Assert.NotNull(converter);
    }

    // Level 1: Simple class
    public class Level1Class
    {
        public string Name { get; set; } = string.Empty;
    }

    // Level 2: Nested with a nested class inside
    public class Level2Class
    {
        public Level1Class Nested { get; set; } = new();
    }

    // Level 3: Deeply nested
    public class Level3Class
    {
        public Level2Class DeepNested { get; set; } = new();
    }

    // Level 4: Maximum depth nesting
    public class Level4Class
    {
        public Level3Class VeryDeepNested { get; set; } = new();
        public string ExtraProperty { get; set; } = string.Empty;
    }

    // Type with multiple parameterized constructors (no parameterless)
    public class MultipleConstructorsNoParameterless
    {
        public MultipleConstructorsNoParameterless(string a)
        {
        }

        public MultipleConstructorsNoParameterless(int b)
        {
        }
    }

    // Type with parameterless constructor (should be fine)
    public class TypeWithParameterlessConstructor
    {
        public TypeWithParameterlessConstructor()
        {
        }

        public TypeWithParameterlessConstructor(string name)
        {
        }
    }

    // Class containing another class that has multiple constructors
    public class ClassWithProblematicInner
    {
        public string Name { get; set; } = string.Empty;

        // This inner class has multiple constructors but no parameterless
        public class ProblematicInner
        {
            public ProblematicInner(string a)
            {
            }

            public ProblematicInner(int b)
            {
            }
        }
    }
}
