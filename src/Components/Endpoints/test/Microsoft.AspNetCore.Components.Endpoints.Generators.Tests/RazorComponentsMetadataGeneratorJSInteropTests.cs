// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Microsoft.AspNetCore.Components.Endpoints.Generators.Tests;

public class RazorComponentsMetadataGeneratorJSInteropTests : RazorComponentsMetadataGeneratorTestBase
{
    [Fact]
    public async Task StaticInstanceCustomIdentifierAndTypedParameters_EmitWorkingDescriptors()
    {
        var result = RunGenerator("""
            namespace TestComponents;

            public sealed class Payload
            {
                public string? Name { get; set; }
            }

            public sealed class InteropTarget
            {
                [Microsoft.JSInterop.JSInvokable]
                public static string Echo(Payload payload) => payload.Name ?? "";

                [Microsoft.JSInterop.JSInvokable("custom-add")]
                public int Add(int value) => value + 2;
            }
            """);

        var context = LoadContext(result, out var loaded);
        using (loaded)
        {
            var methods = GetReferencedJSInvokableMethods(context, result).ToArray();
            Assert.Equal(2, methods.Length);
            var options = CreateJsonOptions();
            var echo = Assert.Single(methods, method => method.Identifier == "Echo");
            Assert.Equal(result.ReferencedAssemblyName, echo.AssemblyName);
            Assert.Equal("TestComponents.InteropTarget", echo.TargetType.FullName);
            Assert.True(echo.IsStatic);
            Assert.Equal("\"Ada\"", await echo.Invoke(null, """[{"Name":"Ada"}]""", options));

            var add = Assert.Single(methods, method => method.Identifier == "custom-add");
            Assert.False(add.IsStatic);
            var target = Activator.CreateInstance(add.TargetType);
            Assert.Equal("5", await add.Invoke(target, "[3]", options));
        }
    }

    [Fact]
    public async Task Arguments_ValidateCountShapeAndObjectReferenceMisuse()
    {
        var result = RunGenerator("""
            namespace TestComponents;

            public sealed class Payload
            {
                public string? Name { get; set; }
            }

            public static class InteropTarget
            {
                [Microsoft.JSInterop.JSInvokable]
                public static string Echo(string value) => value;

                [Microsoft.JSInterop.JSInvokable]
                public static string Read(Payload value) => value.Name ?? "";

                [Microsoft.JSInterop.JSInvokable]
                public static void NoArguments() { }
            }
            """);

        var context = LoadContext(result, out var loaded);
        using (loaded)
        {
            var options = CreateJsonOptions();
            var echo = Assert.Single(context.JSInvokableMethods, method => method.Identifier == "Echo");
            var missing = await Assert.ThrowsAsync<ArgumentException>(
                async () => await echo.Invoke(null, "[]", options));
            Assert.Equal("The call to 'Echo' expects '1' parameters, but received '0'.", missing.Message);
            var extra = await Assert.ThrowsAsync<JsonException>(
                async () => await echo.Invoke(null, """["one","two"]""", options));
            Assert.Equal(
                "Unexpected JSON token String. Ensure that the call to `Echo' is supplied with exactly '1' parameters.",
                extra.Message);

            var read = Assert.Single(context.JSInvokableMethods, method => method.Identifier == "Read");
            var misuse = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await read.Invoke(null, """[{"__dotNetObject":1}]""", options));
            Assert.Equal(
                "In call to 'Read', parameter of type 'Payload' at index 1 must be declared as type 'DotNetObjectRef<Payload>' to receive the incoming value.",
                misuse.Message);

            var noArguments = Assert.Single(context.JSInvokableMethods, method => method.Identifier == "NoArguments");
            await Assert.ThrowsAsync<JsonException>(
                async () => await noArguments.Invoke(null, "[1]", options));
        }
    }

    [Fact]
    public async Task ReturnMatrix_SerializesValuesAndReturnsNullForNoValue()
    {
        var result = RunGenerator("""
            namespace TestComponents;

            public static class ReturnShapes
            {
                [Microsoft.JSInterop.JSInvokable] public static void Void() { }
                [Microsoft.JSInterop.JSInvokable] public static int Value() => 1;
                [Microsoft.JSInterop.JSInvokable] public static System.Threading.Tasks.Task Task() => System.Threading.Tasks.Task.CompletedTask;
                [Microsoft.JSInterop.JSInvokable] public static System.Threading.Tasks.Task<int> TaskValue() => System.Threading.Tasks.Task.FromResult(2);
                [Microsoft.JSInterop.JSInvokable] public static System.Threading.Tasks.ValueTask ValueTask() => System.Threading.Tasks.ValueTask.CompletedTask;
                [Microsoft.JSInterop.JSInvokable] public static System.Threading.Tasks.ValueTask<int> ValueTaskValue() => System.Threading.Tasks.ValueTask.FromResult(3);
            }
            """);

        var context = LoadContext(result, out var loaded);
        using (loaded)
        {
            var options = CreateJsonOptions();
            Assert.Null(await Find("Void").Invoke(null, "[]", options));
            Assert.Equal("1", await Find("Value").Invoke(null, "[]", options));
            Assert.Null(await Find("Task").Invoke(null, "[]", options));
            Assert.Equal("2", await Find("TaskValue").Invoke(null, "[]", options));
            Assert.Null(await Find("ValueTask").Invoke(null, "[]", options));
            Assert.Equal("3", await Find("ValueTaskValue").Invoke(null, "[]", options));

            Microsoft.JSInterop.Infrastructure.JSInvokableMethodDescriptor Find(string identifier)
                => Assert.Single(context.JSInvokableMethods, method => method.Identifier == identifier);
        }
    }

    [Fact]
    public void UnusableMethods_DoNotEmitDescriptors()
    {
        var result = RunGenerator("""
            namespace TestComponents;

            public sealed class InteropTarget
            {
                [Microsoft.JSInterop.JSInvokable]
                public static T Generic<T>(T value) => value;

                [Microsoft.JSInterop.JSInvokable]
                public static void ByReference(ref int value) { }

                [Microsoft.JSInterop.JSInvokable]
                private static int Hidden() => 1;

                [Microsoft.JSInterop.JSInvokable]
                internal static int Internal() => 1;

                [Microsoft.JSInterop.JSInvokable]
                public static int Supported() => 2;
            }

            internal static class InternalTarget
            {
                [Microsoft.JSInterop.JSInvokable]
                public static int PublicMethod() => 4;
            }

            public sealed class GenericTarget<T>
            {
                [Microsoft.JSInterop.JSInvokable]
                public static int GenericContainer() => 3;
            }
            """);

        var context = LoadContext(result, out var loaded);
        using (loaded)
        {
            Assert.Equal(
                "Supported",
                Assert.Single(GetReferencedJSInvokableMethods(context, result)).Identifier);
        }
    }

    [Fact]
    public void Descriptors_AreStablyOrderedByTypeAndIdentifier()
    {
        var result = RunGenerator("""
            namespace TestComponents;

            public static class ZType
            {
                [Microsoft.JSInterop.JSInvokable("beta")] public static void B() { }
                [Microsoft.JSInterop.JSInvokable("alpha")] public static void A() { }
            }

            public static class AType
            {
                [Microsoft.JSInterop.JSInvokable("zeta")] public static void Z() { }
            }
            """);

        var context = LoadContext(result, out var loaded);
        using (loaded)
        {
            Assert.Equal(
                ["TestComponents.AType:zeta", "TestComponents.ZType:alpha", "TestComponents.ZType:beta"],
                GetReferencedJSInvokableMethods(context, result)
                    .Select(method => $"{method.TargetType.FullName}:{method.Identifier}"));
        }
    }

    [Fact]
    public void MultipleAttributes_EmitOneDescriptorPerAlias()
    {
        var result = RunGenerator("""
            namespace TestComponents;

            public static class InteropTarget
            {
                [Microsoft.JSInterop.JSInvokable("first")]
                [Microsoft.JSInterop.JSInvokable("second")]
                public static int Read() => 42;
            }
            """);

        var context = LoadContext(result, out var loaded);
        using (loaded)
        {
            var methods = GetReferencedJSInvokableMethods(context, result);
            Assert.Equal(["first", "second"], methods.Select(method => method.Identifier));
            Assert.Equal(2, methods.Select(method => method.MethodKey).Distinct().Count());
        }
    }

    [Fact]
    public void DuplicateAliases_RemainDistinctContributions()
    {
        var result = RunGenerator("""
            namespace TestComponents;

            public static class InteropTarget
            {
                [Microsoft.JSInterop.JSInvokable("same")]
                [Microsoft.JSInterop.JSInvokable("same")]
                public static int Read() => 42;
            }
            """);

        var context = LoadContext(result, out var loaded);
        using (loaded)
        {
            var descriptors = GetReferencedJSInvokableMethods(context, result).ToArray();
            Assert.Equal(2, descriptors.Length);
            Assert.All(descriptors, method => Assert.Equal("same", method.Identifier));
            Assert.Equal(2, descriptors.Select(method => method.MethodKey).Distinct().Count());
        }
    }

    [Fact]
    public void Overrides_EmitApplicabilityMetadataAndBlockers()
    {
        var result = RunGenerator("""
            namespace TestComponents;

            public class BaseTarget
            {
                [Microsoft.JSInterop.JSInvokable("base-alias")]
                [Microsoft.JSInterop.JSInvokable("shared")]
                public virtual string Read() => "base";

                [Microsoft.JSInterop.JSInvokable("inherited")]
                public string Inherited() => "base";
            }

            public sealed class UnannotatedOverride : BaseTarget
            {
                public override string Read() => "hidden";
            }

            public sealed class AnnotatedOverride : BaseTarget
            {
                [Microsoft.JSInterop.JSInvokable("derived-alias")]
                [Microsoft.JSInterop.JSInvokable("shared")]
                public override string Read() => "derived";
            }

            public sealed class NewSlot : BaseTarget
            {
                [Microsoft.JSInterop.JSInvokable("shared")]
                public new string Read() => "new";
            }
            """);

        var context = LoadContext(result, out var loaded);
        using (loaded)
        {
            Assert.Collection(
                context.JSInvokableMethods
                    .Where(method => method.TargetType.FullName == "TestComponents.UnannotatedOverride" &&
                        method.Identifier.Length > 0)
                    .OrderBy(method => method.Identifier),
                descriptor =>
                {
                    Assert.Equal("base-alias", descriptor.Identifier);
                    Assert.Equal(Microsoft.JSInterop.Infrastructure.JSInvokableMethodKind.OverrideBlocker, descriptor.Kind);
                },
                descriptor =>
                {
                    Assert.Equal("shared", descriptor.Identifier);
                    Assert.Equal(Microsoft.JSInterop.Infrastructure.JSInvokableMethodKind.OverrideBlocker, descriptor.Kind);
                });

            Assert.Collection(
                context.JSInvokableMethods
                    .Where(method => method.TargetType.FullName == "TestComponents.AnnotatedOverride" &&
                        method.Identifier.Length > 0)
                    .OrderBy(method => method.Identifier),
                descriptor =>
                {
                    Assert.Equal("base-alias", descriptor.Identifier);
                    Assert.Equal(Microsoft.JSInterop.Infrastructure.JSInvokableMethodKind.OverrideBlocker, descriptor.Kind);
                },
                descriptor =>
                {
                    Assert.Equal("derived-alias", descriptor.Identifier);
                    Assert.Equal(Microsoft.JSInterop.Infrastructure.JSInvokableMethodKind.Override, descriptor.Kind);
                },
                descriptor =>
                {
                    Assert.Equal("shared", descriptor.Identifier);
                    Assert.Equal(Microsoft.JSInterop.Infrastructure.JSInvokableMethodKind.Override, descriptor.Kind);
                });

            var newSlot = Assert.Single(
                context.JSInvokableMethods,
                method => method.TargetType.FullName == "TestComponents.NewSlot" &&
                    method.Identifier.Length > 0);
            Assert.Equal(Microsoft.JSInterop.Infrastructure.JSInvokableMethodKind.Method, newSlot.Kind);
        }
    }

    [Fact]
    public void SkippedDerivedTypes_EmitCoverageWhenRepresentableAndFailClosedOtherwise()
    {
        var result = RunGenerator("""
            namespace TestComponents;

            public class BaseTarget
            {
                [Microsoft.JSInterop.JSInvokable("virtual")]
                public virtual string Read() => "base";
            }

            public sealed class GenericDerived<T> : BaseTarget
            {
                public override string Read() => "generic";
            }

            public sealed class GenericInherited<T> : BaseTarget
            {
            }

            public class NormalBase
            {
                [Microsoft.JSInterop.JSInvokable("inherited")]
                public string Read() => "base";
            }

            public sealed class NormalDerived : NormalBase
            {
            }

            public sealed class GenericNormalInherited<T> : NormalBase
            {
            }

            public sealed class GenericNewSlot<T> : NormalBase
            {
                [Microsoft.JSInterop.JSInvokable("inherited")]
                public new string Read() => "generic";
            }

            public static class Container
            {
                private sealed class InaccessibleDerived : BaseTarget
                {
                    public override string Read() => "inaccessible";
                }

                private sealed class InaccessibleNewSlot : NormalBase
                {
                    [Microsoft.JSInterop.JSInvokable("inherited")]
                    public new string Read() => "inaccessible";
                }
            }
            """);

        var context = LoadContext(result, out var loaded);
        using (loaded)
        {
            var baseDescriptor = Assert.Single(
                context.JSInvokableMethods,
                method => method.TargetType.FullName == "TestComponents.BaseTarget");
            Assert.Equal(Microsoft.JSInterop.Infrastructure.JSInvokableMethodKind.Override, baseDescriptor.Kind);

            var genericType = loaded.ReferencedAssembly.GetType("TestComponents.GenericDerived`1")!;
            Assert.Collection(
                context.JSInvokableMethods
                    .Where(method => method.TargetType == genericType)
                    .OrderBy(method => method.Identifier),
                descriptor =>
                {
                    Assert.Empty(descriptor.Identifier);
                    Assert.Equal(
                        Microsoft.JSInterop.Infrastructure.JSInvokableMethodKind.OverrideBlocker,
                        descriptor.Kind);
                },
                descriptor =>
                {
                    Assert.Equal("virtual", descriptor.Identifier);
                    Assert.Equal(
                        Microsoft.JSInterop.Infrastructure.JSInvokableMethodKind.OverrideBlocker,
                        descriptor.Kind);
                });

            var inheritedType = loaded.ReferencedAssembly.GetType("TestComponents.GenericInherited`1")!;
            var coverage = Assert.Single(
                context.JSInvokableMethods,
                method => method.TargetType == inheritedType);
            Assert.Empty(coverage.Identifier);
            Assert.Equal(Microsoft.JSInterop.Infrastructure.JSInvokableMethodKind.OverrideBlocker, coverage.Kind);

            var normalDerived = loaded.ReferencedAssembly.GetType("TestComponents.NormalDerived")!;
            var normalCoverage = Assert.Single(
                context.JSInvokableMethods,
                method => method.TargetType == normalDerived);
            Assert.Empty(normalCoverage.Identifier);
            Assert.Equal(Microsoft.JSInterop.Infrastructure.JSInvokableMethodKind.OverrideBlocker, normalCoverage.Kind);

            var genericNormalType = loaded.ReferencedAssembly.GetType("TestComponents.GenericNormalInherited`1")!;
            var genericNormalCoverage = Assert.Single(
                context.JSInvokableMethods,
                method => method.TargetType == genericNormalType);
            Assert.Empty(genericNormalCoverage.Identifier);
            Assert.Equal(
                Microsoft.JSInterop.Infrastructure.JSInvokableMethodKind.OverrideBlocker,
                genericNormalCoverage.Kind);

            Assert.DoesNotContain(
                context.JSInvokableMethods,
                method => method.TargetType.FullName == "TestComponents.GenericNewSlot`1");
            Assert.DoesNotContain(
                context.JSInvokableMethods,
                method => method.TargetType.FullName?.Contains("Inaccessible", StringComparison.Ordinal) is true);
        }
    }

    private static JsonSerializerOptions CreateJsonOptions()
        => new() { TypeInfoResolver = new DefaultJsonTypeInfoResolver() };
}
