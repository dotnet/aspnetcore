// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Microsoft.AspNetCore.Components.Endpoints.Generators.Tests;

[TestClass]
public sealed class RazorComponentsMetadataGeneratorJSInteropTests : RazorComponentsMetadataGeneratorTestBase
{
    [TestMethod]
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
            Assert.HasCount(2, methods);
            var options = CreateJsonOptions();
            var echo = Assert.ContainsSingle(method => method.Identifier == "Echo", methods);
            Assert.AreEqual(result.ReferencedAssemblyName, echo.AssemblyName);
            Assert.AreEqual("TestComponents.InteropTarget", echo.TargetType.FullName);
            Assert.IsTrue(echo.IsStatic);
            Assert.AreEqual("\"Ada\"", await echo.Invoke(null, """[{"Name":"Ada"}]""", options));

            var add = Assert.ContainsSingle(method => method.Identifier == "custom-add", methods);
            Assert.IsFalse(add.IsStatic);
            var target = Activator.CreateInstance(add.TargetType);
            Assert.AreEqual("5", await add.Invoke(target, "[3]", options));
        }
    }

    [TestMethod]
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
            var echo = Assert.ContainsSingle(method => method.Identifier == "Echo", context.JSInvokableMethods);
            var missing = await Assert.ThrowsExactlyAsync<ArgumentException>(
                async () => await echo.Invoke(null, "[]", options));
            Assert.AreEqual("The call to 'Echo' expects '1' parameters, but received '0'.", missing.Message);
            var extra = await Assert.ThrowsExactlyAsync<JsonException>(
                async () => await echo.Invoke(null, """["one","two"]""", options));
            Assert.AreEqual(
                "Unexpected JSON token String. Ensure that the call to `Echo' is supplied with exactly '1' parameters.",
                extra.Message);

            var read = Assert.ContainsSingle(method => method.Identifier == "Read", context.JSInvokableMethods);
            var misuse = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
                async () => await read.Invoke(null, """[{"__dotNetObject":1}]""", options));
            Assert.AreEqual(
                "In call to 'Read', parameter of type 'Payload' at index 1 must be declared as type 'DotNetObjectRef<Payload>' to receive the incoming value.",
                misuse.Message);

            var noArguments = Assert.ContainsSingle(method => method.Identifier == "NoArguments", context.JSInvokableMethods);
            await Assert.ThrowsExactlyAsync<JsonException>(
                async () => await noArguments.Invoke(null, "[1]", options));
        }
    }

    [TestMethod]
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
            Assert.IsNull(await Find("Void").Invoke(null, "[]", options));
            Assert.AreEqual("1", await Find("Value").Invoke(null, "[]", options));
            Assert.IsNull(await Find("Task").Invoke(null, "[]", options));
            Assert.AreEqual("2", await Find("TaskValue").Invoke(null, "[]", options));
            Assert.IsNull(await Find("ValueTask").Invoke(null, "[]", options));
            Assert.AreEqual("3", await Find("ValueTaskValue").Invoke(null, "[]", options));

            Microsoft.JSInterop.Infrastructure.JSInvokableMethodDescriptor Find(string identifier)
                => Assert.ContainsSingle(method => method.Identifier == identifier, context.JSInvokableMethods);
        }
    }

    [TestMethod]
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
            Assert.AreEqual(
                "Supported",
                Assert.ContainsSingle(GetReferencedJSInvokableMethods(context, result)).Identifier);
        }
    }

    [TestMethod]
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
            CollectionAssert.AreEqual(
                new[] { "TestComponents.AType:zeta", "TestComponents.ZType:alpha", "TestComponents.ZType:beta" },
                GetReferencedJSInvokableMethods(context, result)
                    .Select(method => $"{method.TargetType.FullName}:{method.Identifier}")
                    .ToArray());
        }
    }

    [TestMethod]
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
            CollectionAssert.AreEqual(new[] { "first", "second" }, methods.Select(method => method.Identifier).ToArray());
            Assert.HasCount(2, methods.Select(method => method.MethodKey).Distinct());
        }
    }

    [TestMethod]
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
            Assert.HasCount(2, descriptors);
            foreach (var method in descriptors)
            {
                Assert.AreEqual("same", method.Identifier);
            }
            Assert.HasCount(2, descriptors.Select(method => method.MethodKey).Distinct());
        }
    }

    [TestMethod]
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
            var unannotatedOverrides = context.JSInvokableMethods
                .Where(method => method.TargetType.FullName == "TestComponents.UnannotatedOverride" &&
                    method.Identifier.Length > 0)
                .OrderBy(method => method.Identifier)
                .ToArray();
            Assert.HasCount(2, unannotatedOverrides);
            Assert.AreEqual("base-alias", unannotatedOverrides[0].Identifier);
            Assert.AreEqual(
                Microsoft.JSInterop.Infrastructure.JSInvokableMethodKind.OverrideBlocker,
                unannotatedOverrides[0].Kind);
            Assert.AreEqual("shared", unannotatedOverrides[1].Identifier);
            Assert.AreEqual(
                Microsoft.JSInterop.Infrastructure.JSInvokableMethodKind.OverrideBlocker,
                unannotatedOverrides[1].Kind);

            var annotatedOverrides = context.JSInvokableMethods
                .Where(method => method.TargetType.FullName == "TestComponents.AnnotatedOverride" &&
                    method.Identifier.Length > 0)
                .OrderBy(method => method.Identifier)
                .ToArray();
            Assert.HasCount(3, annotatedOverrides);
            Assert.AreEqual("base-alias", annotatedOverrides[0].Identifier);
            Assert.AreEqual(
                Microsoft.JSInterop.Infrastructure.JSInvokableMethodKind.OverrideBlocker,
                annotatedOverrides[0].Kind);
            Assert.AreEqual("derived-alias", annotatedOverrides[1].Identifier);
            Assert.AreEqual(
                Microsoft.JSInterop.Infrastructure.JSInvokableMethodKind.Override,
                annotatedOverrides[1].Kind);
            Assert.AreEqual("shared", annotatedOverrides[2].Identifier);
            Assert.AreEqual(
                Microsoft.JSInterop.Infrastructure.JSInvokableMethodKind.Override,
                annotatedOverrides[2].Kind);

            var newSlot = Assert.ContainsSingle(
                method => method.TargetType.FullName == "TestComponents.NewSlot" &&
                    method.Identifier.Length > 0,
                context.JSInvokableMethods);
            Assert.AreEqual(Microsoft.JSInterop.Infrastructure.JSInvokableMethodKind.Method, newSlot.Kind);
        }
    }

    [TestMethod]
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
            var baseDescriptor = Assert.ContainsSingle(
                method => method.TargetType.FullName == "TestComponents.BaseTarget",
                context.JSInvokableMethods);
            Assert.AreEqual(Microsoft.JSInterop.Infrastructure.JSInvokableMethodKind.Override, baseDescriptor.Kind);

            var genericType = loaded.ReferencedAssembly.GetType("TestComponents.GenericDerived`1")!;
            var genericDescriptors = context.JSInvokableMethods
                .Where(method => method.TargetType == genericType)
                .OrderBy(method => method.Identifier)
                .ToArray();
            Assert.HasCount(2, genericDescriptors);
            Assert.IsEmpty(genericDescriptors[0].Identifier);
            Assert.AreEqual(
                Microsoft.JSInterop.Infrastructure.JSInvokableMethodKind.OverrideBlocker,
                genericDescriptors[0].Kind);
            Assert.AreEqual("virtual", genericDescriptors[1].Identifier);
            Assert.AreEqual(
                Microsoft.JSInterop.Infrastructure.JSInvokableMethodKind.OverrideBlocker,
                genericDescriptors[1].Kind);

            var inheritedType = loaded.ReferencedAssembly.GetType("TestComponents.GenericInherited`1")!;
            var coverage = Assert.ContainsSingle(
                method => method.TargetType == inheritedType,
                context.JSInvokableMethods);
            Assert.IsEmpty(coverage.Identifier);
            Assert.AreEqual(Microsoft.JSInterop.Infrastructure.JSInvokableMethodKind.OverrideBlocker, coverage.Kind);

            var normalDerived = loaded.ReferencedAssembly.GetType("TestComponents.NormalDerived")!;
            var normalCoverage = Assert.ContainsSingle(
                method => method.TargetType == normalDerived,
                context.JSInvokableMethods);
            Assert.IsEmpty(normalCoverage.Identifier);
            Assert.AreEqual(Microsoft.JSInterop.Infrastructure.JSInvokableMethodKind.OverrideBlocker, normalCoverage.Kind);

            var genericNormalType = loaded.ReferencedAssembly.GetType("TestComponents.GenericNormalInherited`1")!;
            var genericNormalCoverage = Assert.ContainsSingle(
                method => method.TargetType == genericNormalType,
                context.JSInvokableMethods);
            Assert.IsEmpty(genericNormalCoverage.Identifier);
            Assert.AreEqual(
                Microsoft.JSInterop.Infrastructure.JSInvokableMethodKind.OverrideBlocker,
                genericNormalCoverage.Kind);

            Assert.DoesNotContain(
                method => method.TargetType.FullName == "TestComponents.GenericNewSlot`1",
                context.JSInvokableMethods);
            Assert.DoesNotContain(
                method => method.TargetType.FullName?.Contains("Inaccessible", StringComparison.Ordinal) is true,
                context.JSInvokableMethods);
        }
    }

    #region Built-in descriptors

    [TestMethod]
    public void FrameworkCallbackProvider_IsIncludedThroughGeneratedUnsafeAccessor()
    {
        var result = RunGenerator("""
            namespace TestComponents;

            public sealed class Marker : Microsoft.AspNetCore.Components.IComponent
            {
                public void Attach(Microsoft.AspNetCore.Components.RenderHandle renderHandle)
                {
                }

                public System.Threading.Tasks.Task SetParametersAsync(
                    Microsoft.AspNetCore.Components.ParameterView parameters)
                    => System.Threading.Tasks.Task.CompletedTask;
            }
            """);

        var context = LoadContext(result, out var loaded);
        using (loaded)
        {
            var descriptors = context.JSInvokableMethods
                .Where(method => method.AssemblyName == "Microsoft.AspNetCore.Components.Web")
                .ToArray();

            Assert.HasCount(7, descriptors);
            CollectionAssert.AreEqual(
                new[]
                {
                    "AddRootComponent",
                    "DispatchEventAsync",
                    "NotifyChange",
                    "OnSpacerAfterVisible",
                    "OnSpacerBeforeVisible",
                    "RemoveRootComponent",
                    "SetRootComponentParameters",
                },
                descriptors.Select(method => method.Identifier).Order().ToArray());
            foreach (var method in descriptors)
            {
                Assert.IsFalse(method.IsStatic);
            }
        }

        var generatedSource = GetGeneratedSource(result);
        Assert.Contains("UnsafeAccessorType(\"Microsoft.JSInterop.Infrastructure.BuiltInJSInvokableMethodDescriptors, Microsoft.AspNetCore.Components.Web\")", generatedSource);
    }

    #endregion

    private static JsonSerializerOptions CreateJsonOptions()
        => new() { TypeInfoResolver = new DefaultJsonTypeInfoResolver() };
}
