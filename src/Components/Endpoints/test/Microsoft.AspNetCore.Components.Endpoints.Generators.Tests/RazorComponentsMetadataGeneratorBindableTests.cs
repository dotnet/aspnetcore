// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Endpoints.Generators.Tests;

[TestClass]
public sealed class RazorComponentsMetadataGeneratorBindableTests : RazorComponentsMetadataGeneratorTestBase
{
    [TestMethod]
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
            var descriptor = Assert.ContainsSingle(context.BindableTypes);
            var model = Activator.CreateInstance(descriptor.Type)!;
            Assert.AreEqual("Ada", Assert.ContainsSingle(member => member.Name == "Name", descriptor.Members).GetValue(model));
            Assert.AreEqual(3, Assert.ContainsSingle(member => member.Name == "Count", descriptor.Members).GetValue(model));
            Assert.AreEqual("property", Assert.ContainsSingle(member => member.Name == "Secret", descriptor.Members).GetValue(model));
            Assert.AreEqual(42, Assert.ContainsSingle(member => member.Name == "_code", descriptor.Members).GetValue(model));
        }
    }

    [TestMethod]
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
            var descriptor = Assert.ContainsSingle(context.BindableTypes);
            var indexer = Assert.ContainsSingle(descriptor.Indexers);
            Assert.AreEqual(typeof(int), indexer.IndexType);
            Assert.AreEqual(typeof(string), indexer.ValueType);
            Assert.AreEqual("item-5", indexer.GetValue(Activator.CreateInstance(descriptor.Type)!, 5));
        }
    }

    [TestMethod]
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
            var descriptor = Assert.ContainsSingle(context.BindableTypes);
            Assert.HasCount(2, descriptor.Members);
            var model = Activator.CreateInstance(descriptor.Type)!;
            var shared = Assert.ContainsSingle(member => member.Name == "Shared", descriptor.Members);
            Assert.AreEqual(typeof(int), shared.MemberType);
            Assert.AreEqual(11, shared.GetValue(model));
            Assert.AreEqual(9, Assert.ContainsSingle(member => member.Name == "Unique", descriptor.Members).GetValue(model));
        }
    }

    [TestMethod]
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
            CollectionAssert.AreEqual(
                new[] { "TestComponents.ArrayOnly", "TestComponents.Child", "TestComponents.CollectionOnly", "TestComponents.Root" },
                context.BindableTypes.Select(descriptor => descriptor.Type.FullName).ToArray());
            Assert.AreEqual(context.BindableTypes.Count, context.BindableTypes.Select(descriptor => descriptor.Type).Distinct().Count());
            Assert.DoesNotContain(descriptor => descriptor.Type == typeof(string), context.BindableTypes);
        }
    }

    [TestMethod]
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
            var descriptor = Assert.ContainsSingle(context.BindableTypes);
            Assert.AreEqual("Supported", Assert.ContainsSingle(descriptor.Members).Name);
            Assert.IsEmpty(descriptor.Indexers);
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
