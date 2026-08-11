// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Microsoft.AspNetCore.Components.Endpoints.Generators.Tests;

#nullable enable

[TestClass]
public sealed class RazorComponentsMetadataGeneratorSerializationTests : RazorComponentsMetadataGeneratorTestBase
{
    [TestMethod]
    public void PersistentStateParameter_RequestsExactlyClosedSerializerAndHandlesMissingRegistration()
    {
        var result = RunGenerator("""
            namespace TestComponents;

            public sealed class State
            {
                public int Value { get; set; }
            }

            public sealed class PersistentComponent : Microsoft.AspNetCore.Components.ComponentBase
            {
                [Microsoft.AspNetCore.Components.PersistentState]
                public State? Saved { get; set; }
            }
            """);

        var source = GetGeneratedSource(result);
        StringAssert.Contains(source, "PersistentComponentStateSerializer<global::TestComponents.State>");
        Assert.IsFalse(source.Contains("MakeGenericType", StringComparison.Ordinal));

        var context = LoadContext(result, out var loaded);
        using (loaded)
        {
            var parameter = Assert.ContainsSingle(Assert.ContainsSingle(context.Components).Parameters);
            Assert.IsNotNull(parameter.GetStateSerializer);

            var marker = new object();
            var provider = new CapturingServiceProvider(marker);
            Assert.AreSame(marker, parameter.GetStateSerializer(provider));
            Assert.AreEqual("Microsoft.AspNetCore.Components.PersistentComponentStateSerializer`1", provider.RequestedType!.GetGenericTypeDefinition().FullName);
            Assert.AreEqual("TestComponents.State", Assert.ContainsSingle(provider.RequestedType.GetGenericArguments()).FullName);
            Assert.IsNull(parameter.GetStateSerializer(new CapturingServiceProvider(null)));
        }
    }

    [TestMethod]
    public void DeclaredJsonResolver_IsPreservedWhileOtherMembersAreGenerated()
    {
        var result = RunGenerator(
            """
            namespace TestComponents;
            public sealed class AppComponent : Microsoft.AspNetCore.Components.ComponentBase { }
            """,
            """
            namespace TestHost;

            public sealed partial class TestMetadata : Microsoft.AspNetCore.Components.Web.RazorComponentsMetadataContext
            {
                public override System.Text.Json.Serialization.Metadata.IJsonTypeInfoResolver? JsonTypeInfoResolver
                    => new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver();
            }
            """);

        var source = GetGeneratedSource(result);
        Assert.IsFalse(source.Contains("JsonTypeInfoResolver => null", StringComparison.Ordinal));
        StringAssert.Contains(source, "public override global::System.Collections.Generic.IReadOnlyList<global::Microsoft.AspNetCore.Components.Infrastructure.ComponentDescriptor> Components");

        var context = LoadContext(result, out var loaded);
        using (loaded)
        {
            Assert.IsInstanceOfType<DefaultJsonTypeInfoResolver>(context.JsonTypeInfoResolver);
            Assert.ContainsSingle(context.Components);
        }
    }

    [TestMethod]
    public async Task MissingJsonResolverAndGeneratedJsonHelpers_UseSuppliedTypedContracts()
    {
        var result = RunGenerator("""
            namespace TestComponents;

            public sealed class ApplicationContract
            {
                public int Value { get; set; }
            }

            public static class ContractInterop
            {
                [Microsoft.JSInterop.JSInvokable]
                public static ApplicationContract RoundTrip(ApplicationContract value) => value;
            }
            """);

        var source = GetGeneratedSource(result);
        StringAssert.Contains(source, "JsonTypeInfoResolver => null");
        StringAssert.Contains(source, "JsonTypeInfo<T>)options.GetTypeInfo(typeof(T))");
        StringAssert.Contains(source, "JsonSerializer.Deserialize(argument, typeInfo)");
        StringAssert.Contains(source, "JsonSerializer.Serialize(value, typeInfo)");

        var context = LoadContext(result, out var loaded);
        using (loaded)
        {
            Assert.IsNull(context.JsonTypeInfoResolver);
            var resolver = new TrackingResolver();
            var options = new JsonSerializerOptions { TypeInfoResolver = resolver };
            var method = Assert.ContainsSingle(GetReferencedJSInvokableMethods(context, result));
            Assert.AreEqual("""{"Value":17}""", await method.Invoke(null, """[{"Value":17}]""", options));
            Assert.Contains(type => type.FullName == "TestComponents.ApplicationContract", resolver.RequestedTypes);
        }
    }

    private sealed class CapturingServiceProvider(object? result) : IServiceProvider
    {
        public Type? RequestedType { get; private set; }

        public object? GetService(Type serviceType)
        {
            RequestedType = serviceType;
            return result;
        }
    }

    private sealed class TrackingResolver : IJsonTypeInfoResolver
    {
        private readonly DefaultJsonTypeInfoResolver _inner = new();

        public List<Type> RequestedTypes { get; } = [];

        public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options)
        {
            RequestedTypes.Add(type);
            return _inner.GetTypeInfo(type, options);
        }
    }
}
