// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.JSInterop.Infrastructure;

namespace Microsoft.JSInterop.SourceGenerators.Tests;

public class JSInvokableGeneratorTests
{
    private const string SupportedSignatures = """
        using System.Threading.Tasks;
        using Microsoft.JSInterop;

        namespace TestApp;

        public class Payload
        {
            public int Value { get; set; }
        }

        public static class StaticMethods
        {
            [JSInvokable]
            public static int Add(int a, int b) => a + b;

            [JSInvokable("first"), JSInvokable("second")]
            public static string Echo(string value) => value;

            [JSInvokable]
            public static void Nothing() { }

            [JSInvokable]
            public static Task CompleteAsync() => Task.CompletedTask;

            [JSInvokable]
            public static ValueTask CompleteValueTaskAsync() => default;

            [JSInvokable]
            public static async Task<Payload> PayloadAsync(Payload payload)
            {
                await Task.Yield();
                return new Payload { Value = payload.Value * 2 };
            }

            [JSInvokable]
            public static ValueTask<int?> NullableAsync(int? value) => new(value);

            [JSInvokable("event")]
            public static int @event(int @class) => @class;
        }

        public static class ObsoleteMethods
        {
            [JSInvokable, System.Obsolete("JavaScript only", error: true)]
            public static int Legacy() => 1;
        }

        internal class InternalInstance
        {
            [JSInvokable]
            public int Increment(int value) => value + 1;

            public class Nested
            {
                [JSInvokable]
                public string Name() => nameof(Nested);
            }
        }
        """;

    [Fact]
    public void SupportedSignatures_GenerateCompilableRegistrations()
    {
        var run = RunGenerator(SupportedSignatures);

        Assert.Empty(run.GeneratorDiagnostics);
        AssertNoCompilationErrors(run.OutputCompilation);
        var source = GetGeneratedSource(run.Result);
        Assert.Contains("[global::System.Runtime.CompilerServices.ModuleInitializer]", source);
        Assert.Contains("if (!global::Microsoft.JSInterop.GeneratedInteropPrototype.GeneratedOnly)", source);
        Assert.Contains("RegisterStatic(\"TestAssembly\", \"first\", 1", source);
        Assert.Contains("RegisterStatic(\"TestAssembly\", \"second\", 1", source);
        Assert.Contains("RegisterInstance<global::TestApp.InternalInstance>(\"Increment\", 1", source);
        Assert.Contains("RegisterInstance<global::TestApp.InternalInstance.Nested>(\"Name\", 0", source);
        Assert.Contains("@event(", source);
    }

    [Fact]
    public async Task GeneratedRegistrations_DispatchThroughDotNetDispatcher()
    {
        var assemblyName = "GeneratedDispatch" + Guid.NewGuid().ToString("N");
        var run = RunGenerator(SupportedSignatures, assemblyName);
        AssertNoCompilationErrors(run.OutputCompilation);
        var assembly = Emit(run.OutputCompilation);
        var runtime = new RecordingRuntime();

        AppContext.SetSwitch("Microsoft.JSInterop.GeneratedOnly", true);
        try
        {
            RuntimeHelpers.RunModuleConstructor(assembly.ManifestModule.ModuleHandle);

            Assert.Equal("5", DotNetDispatcher.Invoke(runtime, new(assemblyName, "Add", 0, null), "[2,3]"));
            Assert.Equal("\"hi\"", DotNetDispatcher.Invoke(runtime, new(assemblyName, "second", 0, null), "[\"hi\"]"));
            Assert.Null(DotNetDispatcher.Invoke(runtime, new(assemblyName, "Nothing", 0, null), "[]"));
            Assert.Equal("7", DotNetDispatcher.Invoke(runtime, new(assemblyName, "event", 0, null), "[7]"));
            Assert.Throws<InvalidOperationException>(() => DotNetDispatcher.Invoke(runtime, new(assemblyName, "PayloadAsync", 0, null), "[{\"value\":1}]"));

            runtime.Completion = new();
            DotNetDispatcher.BeginInvokeDotNet(runtime, new(assemblyName, "PayloadAsync", 0, "1"), "[{\"value\":21}]");
            var result = await runtime.Completion.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.True(result.Success);
            Assert.Equal("{\"value\":42}", result.ResultJson);

            var instance = Activator.CreateInstance(assembly.GetType("TestApp.InternalInstance", throwOnError: true)!)!;
            var reference = CreateReference(instance);
            using var marker = JsonDocument.Parse(JsonSerializer.Serialize(reference, reference.GetType(), runtime.Options));
            var id = marker.RootElement.GetProperty("__dotNetObject").GetInt64();
            Assert.Equal("4", DotNetDispatcher.Invoke(runtime, new(null, "Increment", id, null), "[3]"));
        }
        finally
        {
            AppContext.SetSwitch("Microsoft.JSInterop.GeneratedOnly", false);
        }
    }

    [Fact]
    public void NoSourceIsGeneratedWithoutGeneratedInteropRuntimeSupport()
    {
        var source = """
            namespace Microsoft.JSInterop
            {
                [System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = true)]
                public sealed class JSInvokableAttribute : System.Attribute { }
            }

            public static class Methods
            {
                [Microsoft.JSInterop.JSInvokable]
                public static void Run() { }
            }
            """;

        var run = RunGenerator(source, references: GetFrameworkReferences());

        Assert.Empty(run.GeneratorDiagnostics);
        Assert.Empty(run.Result.GeneratedTrees);
    }

    [Theory]
    [InlineData("public class C { [JSInvokable] internal static void M() { } }", "JSI0001")]
    [InlineData("public class C { [JSInvokable] public static void M<T>() { } }", "JSI0002")]
    [InlineData("public class C<T> { [JSInvokable] public void M() { } }", "JSI0002")]
    [InlineData("public class C { private class P { [JSInvokable] public void M() { } } }", "JSI0003")]
    [InlineData("internal class C { [JSInvokable] public static void M() { } }", "JSI0004")]
    [InlineData("public abstract class C { [JSInvokable] public abstract void M(); }", "JSI0005")]
    [InlineData("public interface I { [JSInvokable] void M(); }", "JSI0005")]
    [InlineData("public interface I { [JSInvokable] public static virtual void M() { } }", "JSI0005")]
    [InlineData("public struct S { [JSInvokable] public void M() { } }", "JSI0005")]
    [InlineData("public class C { [JSInvokable] public static void M(ref int value) { } }", "JSI0005")]
    [InlineData("public class C { [JSInvokable(\"\")] public static void M() { } }", "JSI0005")]
    [InlineData("public class C { [JSInvokable(\"x\")] public static void A() { } [JSInvokable(\"x\")] public static void B() { } }", "JSI0006")]
    public void UnsupportedMethods_ReportDiagnosticsAndAreNotRegistered(string declaration, string diagnosticId)
    {
        var run = RunGenerator("using Microsoft.JSInterop;\n" + declaration);

        Assert.Contains(run.GeneratorDiagnostics, diagnostic => diagnostic.Id == diagnosticId);
        Assert.DoesNotContain(run.Result.GeneratedTrees, tree => tree.GetText().ToString().Contains("Register"));
        AssertNoCompilationErrors(run.OutputCompilation);
    }

    [Fact]
    public void InstanceIdentifiersAreScopedToTheirContainingType()
    {
        var run = RunGenerator("""
            using Microsoft.JSInterop;

            public class A { [JSInvokable] public void Run() { } }
            public class B { [JSInvokable] public void Run() { } }
            """);

        Assert.Empty(run.GeneratorDiagnostics);
        AssertNoCompilationErrors(run.OutputCompilation);
        var source = GetGeneratedSource(run.Result);
        Assert.Contains("RegisterInstance<global::A>(\"Run\"", source);
        Assert.Contains("RegisterInstance<global::B>(\"Run\"", source);
    }

    [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "Test-only reflection over dynamically compiled code.")]
    private static object CreateReference(object value)
        => typeof(DotNetObjectReference)
            .GetMethod(nameof(DotNetObjectReference.Create))!
            .MakeGenericMethod(value.GetType())
            .Invoke(null, [value])!;

    private static Assembly Emit(Compilation compilation)
    {
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        Assert.True(result.Success, string.Join(Environment.NewLine, result.Diagnostics));
        stream.Position = 0;
        return AssemblyLoadContext.Default.LoadFromStream(stream);
    }

    private static GeneratorRun RunGenerator(string source, string assemblyName = "TestAssembly", IEnumerable<MetadataReference>? references = null)
    {
        var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp13);
        var compilation = CSharpCompilation.Create(
            assemblyName,
            [CSharpSyntaxTree.ParseText(source, parseOptions)],
            references ?? GetMetadataReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithNullableContextOptions(NullableContextOptions.Enable));
        GeneratorDriver driver = CSharpGeneratorDriver.Create([new JSInvokableGenerator().AsSourceGenerator()], parseOptions: parseOptions);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var generatorDiagnostics);

        return new GeneratorRun(driver.GetRunResult(), outputCompilation, generatorDiagnostics);
    }

    private static IEnumerable<MetadataReference> GetMetadataReferences()
        => GetFrameworkReferences().Append(MetadataReference.CreateFromFile(typeof(JSInvokableAttribute).Assembly.Location));

    private static IEnumerable<MetadataReference> GetFrameworkReferences()
        => ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Where(path => !string.Equals(Path.GetFileName(path), "Microsoft.JSInterop.dll", StringComparison.OrdinalIgnoreCase))
            .Select(path => MetadataReference.CreateFromFile(path));

    private static void AssertNoCompilationErrors(Compilation compilation)
    {
        var errors = compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToList();
        Assert.True(errors.Count == 0, $"Generated consumer compilation failed:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
    }

    private static string GetGeneratedSource(GeneratorDriverRunResult result)
        => Assert.Single(result.GeneratedTrees, tree => tree.FilePath.EndsWith("JSInvokableRegistrations.g.cs", StringComparison.Ordinal)).GetText().ToString();

    private sealed record GeneratorRun(GeneratorDriverRunResult Result, Compilation OutputCompilation, ImmutableArray<Diagnostic> GeneratorDiagnostics);

    private sealed class RecordingRuntime : JSRuntime
    {
        public TaskCompletionSource<DotNetInvocationResult> Completion { get; set; } = new();

        public JsonSerializerOptions Options => JsonSerializerOptions;

        protected override void BeginInvokeJS(long taskId, string identifier, string? argsJson, JSCallResultType resultType, long targetInstanceId)
            => throw new NotImplementedException();

        protected override void EndInvokeDotNet(DotNetInvocationInfo invocationInfo, in DotNetInvocationResult invocationResult)
            => Completion.TrySetResult(invocationResult);
    }
}
