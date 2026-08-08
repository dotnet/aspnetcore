// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Microsoft.AspNetCore.Components.Endpoints.Generators.Tests;

#nullable enable

public class RazorComponentsMetadataGeneratorSerializationTests : RazorComponentsMetadataGeneratorTestBase
{
    [Fact]
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
        Assert.Contains("PersistentComponentStateSerializer<global::TestComponents.State>", source);
        Assert.DoesNotContain("MakeGenericType", source);

        var context = LoadContext(result, out var loaded);
        using (loaded)
        {
            var parameter = Assert.Single(Assert.Single(GetReferencedComponents(context, result)).Parameters);
            Assert.NotNull(parameter.GetStateSerializer);

            var marker = new object();
            var provider = new CapturingServiceProvider(marker);
            Assert.Same(marker, parameter.GetStateSerializer(provider));
            Assert.Equal("Microsoft.AspNetCore.Components.PersistentComponentStateSerializer`1", provider.RequestedType!.GetGenericTypeDefinition().FullName);
            Assert.Equal("TestComponents.State", Assert.Single(provider.RequestedType.GetGenericArguments()).FullName);
            Assert.Null(parameter.GetStateSerializer(new CapturingServiceProvider(null)));
        }
    }

    [Fact]
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
        Assert.DoesNotContain("JsonTypeInfoResolver => null", source);
        Assert.Contains("public override global::System.Collections.Generic.IReadOnlyList<global::Microsoft.AspNetCore.Components.Infrastructure.ComponentDescriptor> Components", source);

        var context = LoadContext(result, out var loaded);
        using (loaded)
        {
            Assert.IsType<DefaultJsonTypeInfoResolver>(context.JsonTypeInfoResolver);
            Assert.Single(GetReferencedComponents(context, result));
        }
    }

    [Fact]
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
        Assert.Contains("JsonTypeInfoResolver => null", source);
        Assert.Contains("JsonTypeInfo<T>)options.GetTypeInfo(typeof(T))", source);
        Assert.Contains("JsonSerializer.Deserialize(argument, typeInfo)", source);
        Assert.Contains("JsonSerializer.Serialize(value, typeInfo)", source);

        var context = LoadContext(result, out var loaded);
        using (loaded)
        {
            Assert.Null(context.JsonTypeInfoResolver);
            var resolver = new TrackingResolver();
            var options = new JsonSerializerOptions { TypeInfoResolver = resolver };
            var method = Assert.Single(GetReferencedJSInvokableMethods(context, result));
            Assert.Equal("""{"Value":17}""", await method.Invoke(null, """[{"Value":17}]""", options));
            Assert.Contains(resolver.RequestedTypes, type => type.FullName == "TestComponents.ApplicationContract");
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
