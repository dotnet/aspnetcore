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
    [InlineData("public class C<T> { [JSInvokable] public static void M() { } }", "JSI0002")]
    [InlineData("file class C { [JSInvokable] public void M() { } }", "JSI0003")]
    [InlineData("public class C<T> { [JSInvokable] public void M() { } }", "JSI0007")]
    [InlineData("public class C { private partial class P { [JSInvokable] public void M() { } } }", "JSI0007")]
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
    public void HidingOverrideInNonPartialGenericType_ReportsDiagnostic()
    {
        var run = RunGenerator("""
            using Microsoft.JSInterop;

            public class B { [JSInvokable] public virtual void M() { } }
            public class D<T> : B { public override void M() { } }
            """);

        var diagnostic = Assert.Single(run.GeneratorDiagnostics);
        Assert.Equal("JSI0007", diagnostic.Id);
        Assert.DoesNotContain("RegisterNonInvokableInstance", GetGeneratedSource(run.Result));
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PartialHosts_DispatchWithoutGeneratedStaticConstructor(bool privateHost)
    {
        var source = """
            using System.Threading.Tasks;
            using Microsoft.JSInterop;

            namespace TestApp;

            public partial class Box<T>
            {
                public T Value { get; set; } = default!;

                [JSInvokable]
                public T Swap(T value)
                {
                    var previous = Value;
                    Value = value;
                    return previous;
                }

                [JSInvokable]
                public Task<T> GetAsync() => Task.FromResult(Value);
            }

            public partial class Outer
            {
                public static object CreateInner() => new Inner();

                private partial class Inner
                {
                    [JSInvokable]
                    public string Name() => nameof(Inner);
                }
            }
            """;
        var run = RunGenerator(source, "GenericDispatch" + Guid.NewGuid().ToString("N"));
        Assert.Empty(run.GeneratorDiagnostics);
        AssertNoCompilationErrors(run.OutputCompilation);
        Assert.DoesNotContain(run.Result.GeneratedTrees, tree => tree.FilePath.EndsWith("JSInvokableRegistrations.g.cs", StringComparison.Ordinal));
        var assembly = Emit(run.OutputCompilation);
        var runtime = new RecordingRuntime();
        var openBox = assembly.GetType("TestApp.Box`1", throwOnError: true)!;
        var intBox = Activator.CreateInstance(openBox.MakeGenericType(typeof(int)))!;
        var stringBox = Activator.CreateInstance(openBox.MakeGenericType(typeof(string)))!;
        var inner = assembly.GetType("TestApp.Outer", throwOnError: true)!.GetMethod("CreateInner")!.Invoke(null, null)!;
        var host = privateHost ? inner.GetType() : openBox;
        Assert.Null(host.TypeInitializer);
        Assert.True(host.Attributes.HasFlag(TypeAttributes.BeforeFieldInit));
        Assert.Contains(host.GetCustomAttributesData(), attribute => attribute.AttributeType.Name.StartsWith("__JSInvokableHost_", StringComparison.Ordinal));

        AppContext.SetSwitch("Microsoft.JSInterop.GeneratedOnly", true);
        try
        {
            if (privateHost)
            {
                Assert.Equal("\"Inner\"", DotNetDispatcher.Invoke(runtime, new(null, "Name", Track(runtime, inner), null), "[]"));
                return;
            }

            Assert.Equal("0", DotNetDispatcher.Invoke(runtime, new(null, "Swap", Track(runtime, intBox), null), "[5]"));
            Assert.Equal("5", DotNetDispatcher.Invoke(runtime, new(null, "Swap", Track(runtime, intBox), null), "[6]"));
            Assert.Null(DotNetDispatcher.Invoke(runtime, new(null, "Swap", Track(runtime, stringBox), null), "[\"a\"]"));

            runtime.Completion = new();
            DotNetDispatcher.BeginInvokeDotNet(runtime, new(null, "GetAsync", Track(runtime, stringBox), "1"), "[]");
            var result = await runtime.Completion.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.True(result.Success);
            Assert.Equal("\"a\"", result.ResultJson);
        }
        finally
        {
            AppContext.SetSwitch("Microsoft.JSInterop.GeneratedOnly", false);
        }
    }

    [Theory]
    [InlineData("public partial class G<T> { static G() { } [JSInvokable] public void M() { } }")]
    [InlineData("public partial class G<T> { [JSInvokable, System.Obsolete(\"x\", error: true)] public void M() { } }")]
    [InlineData("public ref partial struct S { private partial class Inner { [JSInvokable] public void M() { } } }")]
    [InlineData("public partial record struct R { private partial record Inner { [JSInvokable] public void M() { } } }")]
    public void GeneratedHosts_Compile(string source)
    {
        var run = RunGenerator("using Microsoft.JSInterop;\n" + source, "HostCompiles");
        Assert.Empty(run.GeneratorDiagnostics);
        Assert.Contains(run.Result.GeneratedTrees, tree => tree.FilePath.EndsWith(".JSInvokable.g.cs", StringComparison.Ordinal));
        AssertNoCompilationErrors(run.OutputCompilation);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OverridesWithoutJSInvokable_MatchReflectionDispatch(bool genericHosts)
    {
        var source = """
            using Microsoft.JSInterop;

            namespace TestApp;

            public class Base
            {
                [JSInvokable]
                public virtual string Hidden() => "base";

                [JSInvokable]
                public virtual string Redeclared() => "base";

                [JSInvokable]
                public virtual string Inherited() => "base";
            }

            public class Derived : Base
            {
                public override string Hidden() => "derived";

                [JSInvokable]
                public override string Redeclared() => "derived";
            }

            public class MoreDerived : Derived
            {
                public override string Inherited() => "more derived";
            }
            """;
        if (genericHosts)
        {
            source = source.Replace("class Base", "partial class Base<T>", StringComparison.Ordinal)
                .Replace("class Derived : Base", "partial class Derived<U> : Base<U>", StringComparison.Ordinal)
                .Replace("class MoreDerived : Derived", "partial class MoreDerived<V> : Derived<V>", StringComparison.Ordinal);
        }

        // Module initializers run once per assembly, so each dispatch mode uses its own assembly.
        var run = RunGenerator(source, "OverrideDispatch" + Guid.NewGuid().ToString("N"));
        var reflectionRun = RunGenerator(source, "OverrideReflectionDispatch" + Guid.NewGuid().ToString("N"));
        Assert.Empty(run.GeneratorDiagnostics);
        AssertNoCompilationErrors(run.OutputCompilation);
        if (!genericHosts)
        {
            var generated = GetGeneratedSource(run.Result);
            Assert.Contains("RegisterNonInvokableInstance<global::TestApp.Derived>(\"Hidden\")", generated);
            Assert.Contains("RegisterNonInvokableInstance<global::TestApp.MoreDerived>(\"Inherited\")", generated);
            Assert.DoesNotContain("RegisterNonInvokableInstance<global::TestApp.Derived>(\"Redeclared\")", generated);
        }

        var reflection = Dispatch(Emit(reflectionRun.OutputCompilation), generatedOnly: false, genericHosts);
        var generatedDispatch = Dispatch(Emit(run.OutputCompilation), generatedOnly: true, genericHosts);

        Assert.Equal(reflection, generatedDispatch);
        Assert.Equal(
            [
                "Base.Hidden: \"base\"",
                "Base.Inherited: \"base\"",
                "Base.Redeclared: \"base\"",
                "Derived.Hidden: ArgumentException",
                "Derived.Inherited: \"base\"",
                "Derived.Redeclared: \"derived\"",
                "MoreDerived.Hidden: ArgumentException",
                "MoreDerived.Inherited: ArgumentException",
                "MoreDerived.Redeclared: \"derived\"",
            ],
            generatedDispatch);

        static List<string> Dispatch(Assembly assembly, bool generatedOnly, bool genericHosts)
        {
            var runtime = new RecordingRuntime();
            AppContext.SetSwitch("Microsoft.JSInterop.GeneratedOnly", generatedOnly);
            try
            {
                if (generatedOnly)
                {
                    RuntimeHelpers.RunModuleConstructor(assembly.ManifestModule.ModuleHandle);
                }

                var results = new List<string>();
                foreach (var typeName in new[] { "Base", "Derived", "MoreDerived" })
                {
                    var type = assembly.GetType("TestApp." + typeName + (genericHosts ? "`1" : string.Empty), throwOnError: true)!;
                    var instance = Activator.CreateInstance(genericHosts ? type.MakeGenericType(typeof(int)) : type)!;
                    foreach (var method in new[] { "Hidden", "Inherited", "Redeclared" })
                    {
                        string result;
                        try
                        {
                            result = DotNetDispatcher.Invoke(runtime, new(null, method, Track(runtime, instance), null), "[]")!;
                        }
                        catch (ArgumentException ex)
                        {
                            Assert.Contains("does not contain a public invokable method", ex.Message);
                            result = nameof(ArgumentException);
                        }

                        results.Add($"{typeName}.{method}: {result}");
                    }
                }

                return results;
            }
            finally
            {
                AppContext.SetSwitch("Microsoft.JSInterop.GeneratedOnly", false);
            }
        }
    }

    [Fact]
    public void GenericHostInheritsCallbacksFromGeneratedReferencedAssembly()
    {
        var baseRun = RunGenerator("""
            using Microsoft.JSInterop;
            public partial class Base<T>
            {
                [JSInvokable]
                public virtual int Inherited() => 1;
            }
            """, "GenericBase" + Guid.NewGuid().ToString("N"));
        Assert.Empty(baseRun.GeneratorDiagnostics);
        AssertNoCompilationErrors(baseRun.OutputCompilation);
        Emit(baseRun.OutputCompilation);
        var derivedRun = RunGenerator("""
            using Microsoft.JSInterop;
            public partial class Derived<U> : Base<U>
            {
                [JSInvokable]
                public int Own() => 2;
            }
            """, "GenericDerived" + Guid.NewGuid().ToString("N"),
            GetMetadataReferences().Append(baseRun.OutputCompilation.ToMetadataReference()));
        Assert.Empty(derivedRun.GeneratorDiagnostics);
        AssertNoCompilationErrors(derivedRun.OutputCompilation);
        var assembly = Emit(derivedRun.OutputCompilation);
        var instance = Activator.CreateInstance(assembly.GetType("Derived`1", throwOnError: true)!.MakeGenericType(typeof(int)))!;
        var runtime = new RecordingRuntime();

        AppContext.SetSwitch("Microsoft.JSInterop.GeneratedOnly", true);
        try
        {
            var id = Track(runtime, instance);
            Assert.Equal("1", DotNetDispatcher.Invoke(runtime, new(null, "Inherited", id, null), "[]"));
            Assert.Equal("2", DotNetDispatcher.Invoke(runtime, new(null, "Own", id, null), "[]"));
        }
        finally
        {
            AppContext.SetSwitch("Microsoft.JSInterop.GeneratedOnly", false);
        }
    }

    private static long Track(RecordingRuntime runtime, object value)
    {
        var reference = CreateReference(value);
        using var marker = JsonDocument.Parse(JsonSerializer.Serialize(reference, reference.GetType(), runtime.Options));
        return marker.RootElement.GetProperty("__dotNetObject").GetInt64();
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
