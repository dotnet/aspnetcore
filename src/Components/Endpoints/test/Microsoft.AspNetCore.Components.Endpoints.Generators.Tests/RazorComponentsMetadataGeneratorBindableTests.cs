// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Endpoints.Generators.Tests;

public class RazorComponentsMetadataGeneratorBindableTests : RazorComponentsMetadataGeneratorTestBase
{
    [Fact]
    public void PublicAndPrivatePropertiesAndFields_EmitWorkingMemberDescriptors()
    {
        var result = RunGenerator("""
            namespace TestComponents;

            public sealed class FormModel
            {
                public string Name { get; set; } = "Ada";
                public int Count = 3;
                private string Secret { get; } = "property";
                private int _code = 42;
            }
            """, HostFor("TestComponents.FormModel"));

        var source = GetGeneratedSource(result);
        Assert.Contains("Name = \"get_Secret\"", source);
        Assert.Contains("UnsafeAccessorKind.Field, Name = \"_code\"", source);

        var context = LoadContext(result, out var loaded);
        using (loaded)
        {
            var descriptor = Assert.Single(context.BindableTypes);
            var model = Activator.CreateInstance(descriptor.Type)!;
            Assert.Equal("Ada", Assert.Single(descriptor.Members, member => member.Name == "Name").GetValue(model));
            Assert.Equal(3, Assert.Single(descriptor.Members, member => member.Name == "Count").GetValue(model));
            Assert.Equal("property", Assert.Single(descriptor.Members, member => member.Name == "Secret").GetValue(model));
            Assert.Equal(42, Assert.Single(descriptor.Members, member => member.Name == "_code").GetValue(model));
        }
    }

    [Fact]
    public void SingleArgumentIndexer_EmitsTypesAndWorkingGetter()
    {
        var result = RunGenerator("""
            namespace TestComponents;

            public sealed class IndexedModel
            {
                public string this[int index] => $"item-{index}";
            }
            """, HostFor("TestComponents.IndexedModel"));

        var context = LoadContext(result, out var loaded);
        using (loaded)
        {
            var descriptor = Assert.Single(context.BindableTypes);
            var indexer = Assert.Single(descriptor.Indexers);
            Assert.Equal(typeof(int), indexer.IndexType);
            Assert.Equal(typeof(string), indexer.ValueType);
            Assert.Equal("item-5", indexer.GetValue(Activator.CreateInstance(descriptor.Type)!, 5));
        }
    }

    [Fact]
    public void InheritedHiddenMembers_UseMostDerivedMemberOnce()
    {
        var result = RunGenerator("""
            namespace TestComponents;

            public class BaseModel
            {
                public string Shared { get; } = "base";
                public int Unique { get; } = 9;
            }

            public sealed class DerivedModel : BaseModel
            {
                public new int Shared { get; } = 11;
            }
            """, HostFor("TestComponents.DerivedModel"));

        var context = LoadContext(result, out var loaded);
        using (loaded)
        {
            var descriptor = Assert.Single(context.BindableTypes);
            Assert.Equal(2, descriptor.Members.Count);
            var model = Activator.CreateInstance(descriptor.Type)!;
            var shared = Assert.Single(descriptor.Members, member => member.Name == "Shared");
            Assert.Equal(typeof(int), shared.MemberType);
            Assert.Equal(11, shared.GetValue(model));
            Assert.Equal(9, Assert.Single(descriptor.Members, member => member.Name == "Unique").GetValue(model));
        }
    }

    [Fact]
    public void TransitiveCyclicGraph_DescribesApplicationTypesOnceInStableOrder()
    {
        var result = RunGenerator("""
            namespace TestComponents;

            public sealed class Root
            {
                public Child? Child { get; set; }
                public ArrayOnly[] Items { get; set; } = [];
                public System.Collections.Generic.List<CollectionOnly> Collection { get; set; } = [];
            }

            public sealed class Child
            {
                public Root? Parent { get; set; }
            }

            public sealed class ArrayOnly
            {
                public string Value { get; set; } = "";
            }

            public sealed class CollectionOnly
            {
                public int Value { get; set; }
            }
            """, HostFor("TestComponents.Root"));

        var context = LoadContext(result, out var loaded);
        using (loaded)
        {
            Assert.Equal(
                ["TestComponents.ArrayOnly", "TestComponents.Child", "TestComponents.CollectionOnly", "TestComponents.Root"],
                context.BindableTypes.Select(descriptor => descriptor.Type.FullName));
            Assert.Equal(context.BindableTypes.Count, context.BindableTypes.Select(descriptor => descriptor.Type).Distinct().Count());
            Assert.DoesNotContain(context.BindableTypes, descriptor => descriptor.Type == typeof(string));
        }
    }

    [Fact]
    public void UnsupportedAndInaccessibleMembers_AreOmittedAndOutputCompiles()
    {
        var result = RunGenerator("""
            namespace TestComponents;

            internal sealed class HiddenValue
            {
            }

            public sealed class OddModel
            {
                internal HiddenValue Hidden { get; } = new();
                public string this[int row, int column] => $"{row}:{column}";
                public string this[string key] { set { } }
                public int Supported { get; } = 5;
            }
            """, HostFor("TestComponents.OddModel"));

        var context = LoadContext(result, out var loaded);
        using (loaded)
        {
            var descriptor = Assert.Single(context.BindableTypes);
            Assert.Equal("Supported", Assert.Single(descriptor.Members).Name);
            Assert.Empty(descriptor.Indexers);
        }
    }

    private static string HostFor(string modelType)
        => $$"""
            namespace TestHost;

            [Microsoft.AspNetCore.Components.Web.BindableModel(ModelType = typeof({{modelType}}))]
            public sealed partial class TestMetadata : Microsoft.AspNetCore.Components.Web.RazorComponentsMetadataContext
            {
            }
            """;
}
