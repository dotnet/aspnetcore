// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

namespace Microsoft.AspNetCore.OpenApi.SourceGenerators.Tests;

[UsesVerify]
public static partial class SnapshotTestHelper
{
    [GeneratedRegex(@"\[global::System\.Runtime\.CompilerServices\.InterceptsLocationAttribute\([^)]*\)\]")]
    private static partial Regex InterceptsLocationRegex();

    private static readonly CSharpParseOptions ParseOptions = new CSharpParseOptions(LanguageVersion.Preview)
        .WithFeatures([new KeyValuePair<string, string>("InterceptorsNamespaces", "Microsoft.AspNetCore.OpenApi.Generated")]);

    public static Task Verify(string source, IIncrementalGenerator generator, out Compilation compilation)
        => Verify(source, generator, [], out compilation, out _);

    public static Task Verify(string source, IIncrementalGenerator generator, Dictionary<string, List<string>> classLibrarySources, out Compilation compilation, out List<byte[]> generatedAssemblies)
    {
        var references = AppDomain.CurrentDomain.GetAssemblies()
                .Where(assembly => !assembly.IsDynamic && !string.IsNullOrWhiteSpace(assembly.Location))
                .Select(assembly => MetadataReference.CreateFromFile(assembly.Location))
                .Concat(
                [
                    MetadataReference.CreateFromFile(typeof(Builder.WebApplicationBuilder).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(OpenApiOptions).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(Builder.EndpointRouteBuilderExtensions).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(Builder.IApplicationBuilder).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(Mvc.ApiExplorer.IApiDescriptionProvider).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(Mvc.ControllerBase).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(MvcServiceCollectionExtensions).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(MvcCoreMvcBuilderExtensions).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(Http.TypedResults).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(System.Text.Json.Nodes.JsonArray).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(Uri).Assembly.Location),
                ])
                .ToList();

        var additionalTexts = new List<AdditionalText>();
        generatedAssemblies = [];

        foreach (var classLibrary in classLibrarySources)
        {
            var classLibraryCompilation = CSharpCompilation.Create(classLibrary.Key,
                classLibrary.Value.Select((source, index) => CSharpSyntaxTree.ParseText(source, options: ParseOptions, path: $"{classLibrary.Key}-{index}.cs")),
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var ms = new MemoryStream();
            using var xmlStream = new MemoryStream();
            var emitResult = classLibraryCompilation.Emit(ms, xmlDocumentationStream: xmlStream);

            if (!emitResult.Success)
            {
                throw new InvalidOperationException($"Failed to compile class library {classLibrary.Key}: {string.Join(Environment.NewLine, emitResult.Diagnostics)}");
            }

            ms.Seek(0, SeekOrigin.Begin);
            xmlStream.Seek(0, SeekOrigin.Begin);

            var assembly = ms.ToArray();
            generatedAssemblies.Add(assembly);
            references.Add(MetadataReference.CreateFromImage(assembly));

            var xmlText = Encoding.UTF8.GetString(xmlStream.ToArray());
            additionalTexts.Add(new TestAdditionalText($"{classLibrary.Key}.xml", xmlText));
        }

        var inputCompilation = CSharpCompilation.Create("OpenApiXmlCommentGeneratorSample",
            [CSharpSyntaxTree.ParseText(source, options: ParseOptions, path: "Program.cs")],
            references,
            new CSharpCompilationOptions(OutputKind.ConsoleApplication));

        var driver = CSharpGeneratorDriver.Create(
            generators: [generator.AsSourceGenerator()],
            additionalTexts: additionalTexts,
            parseOptions: ParseOptions);

        return Verifier
            .Verify(driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out compilation, out var diagnostics))
            .ScrubLinesWithReplace(line => InterceptsLocationRegex().Replace(line, "[InterceptsLocation]"))
            .UseDirectory(SkipOnHelixAttribute.OnHelix()
                ? Path.Combine(Environment.GetEnvironmentVariable("HELIX_WORKITEM_ROOT"), "snapshots")
                : "snapshots");
    }

    public static async Task VerifyOpenApi(Compilation compilation, Action<OpenApiDocument> verifyFunc)
        => await VerifyOpenApi(compilation, [], verifyFunc);

    public static async Task VerifyOpenApi(Compilation compilation, List<byte[]> generatedAssemblies, Action<OpenApiDocument> verifyFunc)
    {
        var assemblyName = compilation.AssemblyName;
        var symbolsName = Path.ChangeExtension(assemblyName, "pdb");

        var output = new MemoryStream();
        var pdb = new MemoryStream();

        var emitOptions = new EmitOptions(
            debugInformationFormat: DebugInformationFormat.PortablePdb,
            pdbFilePath: symbolsName,
            outputNameOverride: $"TestProject-{Guid.NewGuid()}");

        var embeddedTexts = new List<EmbeddedText>();

        foreach (var syntaxTree in compilation.SyntaxTrees)
        {
            var text = syntaxTree.GetText();
            var encoding = text.Encoding ?? Encoding.UTF8;
            var buffer = encoding.GetBytes(text.ToString());
            var sourceText = SourceText.From(buffer, buffer.Length, encoding, canBeEmbedded: true);

            var syntaxRootNode = (CSharpSyntaxNode)syntaxTree.GetRoot();
            var newSyntaxTree = CSharpSyntaxTree.Create(syntaxRootNode, options: ParseOptions, encoding: encoding, path: syntaxTree.FilePath);

            compilation = compilation.ReplaceSyntaxTree(syntaxTree, newSyntaxTree);

            embeddedTexts.Add(EmbeddedText.FromSource(syntaxTree.FilePath, sourceText));
        }

        var result = compilation.Emit(output, pdb, options: emitOptions, embeddedTexts: embeddedTexts);

        Assert.Empty(result.Diagnostics.Where(d => d.Severity > DiagnosticSeverity.Warning));
        Assert.True(result.Success);

        output.Position = 0;
        pdb.Position = 0;

        var assembly = AssemblyLoadContext.Default.LoadFromStream(output, pdb);
        foreach (var generatedAssembly in generatedAssemblies)
        {
            AssemblyLoadContext.Default.LoadFromStream(new MemoryStream(generatedAssembly));
        }

        void ConfigureHostBuilder(object hostBuilder)
        {
            ((IHostBuilder)hostBuilder).ConfigureServices((context, services) =>
            {
                services.AddSingleton<IServer, NoopServer>();
                services.AddSingleton<IHostLifetime, NoopHostLifetime>();
            });
        }

        var waitForStartTcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
        void OnEntryPointExit(Exception exception)
        {
            // If the entry point exited, we'll try to complete the wait
            if (exception != null)
            {
                waitForStartTcs.TrySetException(exception);
            }
            else
            {
                waitForStartTcs.TrySetResult(0);
            }
        }

        var factory = HostFactoryResolver.ResolveHostFactory(assembly,
            stopApplication: false,
            configureHostBuilder: ConfigureHostBuilder,
            entrypointCompleted: OnEntryPointExit);

        if (factory == null)
        {
            return;
        }

        var services = ((IHost)factory([$"--{HostDefaults.ApplicationKey}={assemblyName}"])).Services;

        var applicationLifetime = services.GetRequiredService<IHostApplicationLifetime>();
        using (var registration = applicationLifetime.ApplicationStarted.Register(() => waitForStartTcs.TrySetResult(0)))
        {
            waitForStartTcs.Task.Wait();
            var targetAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(assembly => assembly.GetName().Name == "Microsoft.AspNetCore.OpenApi");
            var serviceType = targetAssembly.GetType("Microsoft.Extensions.ApiDescriptions.IDocumentProvider", throwOnError: false);

            if (serviceType == null)
            {
                return;
            }

            var service = services.GetService(serviceType) ?? throw new InvalidOperationException("Could not resolve IDocumentProvider service.");
            using var stream = new MemoryStream();
            var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
            using var writer = new StreamWriter(stream, encoding, bufferSize: 1024, leaveOpen: true);
            var targetMethod = serviceType.GetMethod("GenerateAsync", [typeof(string), typeof(TextWriter)]) ?? throw new InvalidOperationException("Could not resolve GenerateAsync method.");
            targetMethod.Invoke(service, ["v1", writer]);
            stream.Position = 0;
            var (document, _) = await OpenApiDocument.LoadAsync(stream, "json");
            verifyFunc(document);
        }
    }

    private sealed class NoopHostLifetime : IHostLifetime
    {
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task WaitForStartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class NoopServer : IServer
    {
        public IFeatureCollection Features { get; } = new FeatureCollection();
        public void Dispose() { }
        public Task StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken) where TContext : notnull => Task.CompletedTask;
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class HostFactoryResolver
    {
        private const BindingFlags DeclaredOnlyLookup = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

        public const string BuildWebHost = nameof(BuildWebHost);
        public const string CreateWebHostBuilder = nameof(CreateWebHostBuilder);
        public const string CreateHostBuilder = nameof(CreateHostBuilder);
        private const string TimeoutEnvironmentKey = "DOTNET_HOST_FACTORY_RESOLVER_DEFAULT_TIMEOUT_IN_SECONDS";

        // The amount of time we wait for the diagnostic source events to fire
        private static readonly TimeSpan s_defaultWaitTimeout = SetupDefaultTimeout();

        private static TimeSpan SetupDefaultTimeout()
        {
            if (Debugger.IsAttached)
            {
                return Timeout.InfiniteTimeSpan;
            }

            if (uint.TryParse(Environment.GetEnvironmentVariable(TimeoutEnvironmentKey), out uint timeoutInSeconds))
            {
                return TimeSpan.FromSeconds((int)timeoutInSeconds);
            }

            return TimeSpan.FromMinutes(5);
        }

        public static Func<string[], TWebHost> ResolveWebHostFactory<TWebHost>(Assembly assembly)
        {
            return ResolveFactory<TWebHost>(assembly, BuildWebHost);
        }

        public static Func<string[], TWebHostBuilder> ResolveWebHostBuilderFactory<TWebHostBuilder>(Assembly assembly)
        {
            return ResolveFactory<TWebHostBuilder>(assembly, CreateWebHostBuilder);
        }

        public static Func<string[], THostBuilder> ResolveHostBuilderFactory<THostBuilder>(Assembly assembly)
        {
            return ResolveFactory<THostBuilder>(assembly, CreateHostBuilder);
        }

        // This helpers encapsulates all of the complex logic required to:
        // 1. Execute the entry point of the specified assembly in a different thread.
        // 2. Wait for the diagnostic source events to fire
        // 3. Give the caller a chance to execute logic to mutate the IHostBuilder
        // 4. Resolve the instance of the applications's IHost
        // 5. Allow the caller to determine if the entry point has completed
        public static Func<string[], object> ResolveHostFactory(Assembly assembly,
                                                                 TimeSpan waitTimeout = default,
                                                                 bool stopApplication = true,
                                                                 Action<object> configureHostBuilder = null,
                                                                 Action<Exception> entrypointCompleted = null)
        {
            if (assembly.EntryPoint is null)
            {
                return null;
            }

            return args => new HostingListener(args, assembly.EntryPoint, waitTimeout == default ? s_defaultWaitTimeout : waitTimeout, stopApplication, configureHostBuilder, entrypointCompleted).CreateHost();
        }

        private static Func<string[], T> ResolveFactory<T>(Assembly assembly, string name)
        {
            var programType = assembly.EntryPoint.DeclaringType;
            if (programType == null)
            {
                return null;
            }

            var factory = programType.GetMethod(name, DeclaredOnlyLookup);
            if (!IsFactory<T>(factory))
            {
                return null;
            }

            return args => (T)factory!.Invoke(null, [args])!;
        }

        // TReturn Factory(string[] args);
        private static bool IsFactory<TReturn>(MethodInfo factory)
        {
            return factory != null
                && typeof(TReturn).IsAssignableFrom(factory.ReturnType)
                && factory.GetParameters().Length == 1
                && typeof(string[]).Equals(factory.GetParameters()[0].ParameterType);
        }

        // Used by EF tooling without any Hosting references. Looses some return type safety checks.
        public static Func<string[], IServiceProvider> ResolveServiceProviderFactory(Assembly assembly, TimeSpan waitTimeout = default)
        {
            // Prefer the older patterns by default for back compat.
            var webHostFactory = ResolveWebHostFactory<object>(assembly);
            if (webHostFactory != null)
            {
                return args =>
                {
                    var webHost = webHostFactory(args);
                    return GetServiceProvider(webHost);
                };
            }

            var webHostBuilderFactory = ResolveWebHostBuilderFactory<object>(assembly);
            if (webHostBuilderFactory != null)
            {
                return args =>
                {
                    var webHostBuilder = webHostBuilderFactory(args);
                    var webHost = Build(webHostBuilder);
                    return GetServiceProvider(webHost);
                };
            }

            var hostBuilderFactory = ResolveHostBuilderFactory<object>(assembly);
            if (hostBuilderFactory != null)
            {
                return args =>
                {
                    var hostBuilder = hostBuilderFactory(args);
                    var host = Build(hostBuilder);
                    return GetServiceProvider(host);
                };
            }

            var hostFactory = ResolveHostFactory(assembly, waitTimeout: waitTimeout);
            if (hostFactory != null)
            {
                return args =>
                {
                    static bool IsApplicationNameArg(string arg)
                        => arg.Equals("--applicationName", StringComparison.OrdinalIgnoreCase) ||
                            arg.Equals("/applicationName", StringComparison.OrdinalIgnoreCase);

                    if (!args.Any(arg => IsApplicationNameArg(arg)) && assembly.GetName().Name is string assemblyName)
                    {
                        args = [.. args, .. new[] { "--applicationName", assemblyName }];
                    }

                    var host = hostFactory(args);
                    return GetServiceProvider(host);
                };
            }

            return null;
        }

        private static object Build(object builder)
        {
            var buildMethod = builder.GetType().GetMethod("Build");
            return buildMethod.Invoke(builder, []);
        }

        private static IServiceProvider GetServiceProvider(object host)
        {
            if (host == null)
            {
                return null;
            }
            var hostType = host.GetType();
            var servicesProperty = hostType.GetProperty("Services", DeclaredOnlyLookup);
            return (IServiceProvider)servicesProperty.GetValue(host);
        }

        private sealed class HostingListener : IObserver<DiagnosticListener>, IObserver<KeyValuePair<string, object>>
        {
            private readonly string[] _args;
            private readonly MethodInfo _entryPoint;
            private readonly TimeSpan _waitTimeout;
            private readonly bool _stopApplication;

            private readonly TaskCompletionSource<object> _hostTcs = new();
            private IDisposable _disposable;
            private readonly Action<object> _configure;
            private readonly Action<Exception> _entrypointCompleted;
            private static readonly AsyncLocal<HostingListener> _currentListener = new();

            public HostingListener(string[] args, MethodInfo entryPoint, TimeSpan waitTimeout, bool stopApplication, Action<object> configure, Action<Exception> entrypointCompleted)
            {
                _args = args;
                _entryPoint = entryPoint;
                _waitTimeout = waitTimeout;
                _stopApplication = stopApplication;
                _configure = configure;
                _entrypointCompleted = entrypointCompleted;
            }

            public object CreateHost()
            {
                using var subscription = DiagnosticListener.AllListeners.Subscribe(this);

                // Kick off the entry point on a new thread so we don't block the current one
                // in case we need to timeout the execution
                var thread = new Thread(() =>
                {
                    Exception exception = null;

                    try
                    {
                        // Set the async local to the instance of the HostingListener so we can filter events that
                        // aren't scoped to this execution of the entry point.
                        _currentListener.Value = this;

                        var parameters = _entryPoint.GetParameters();
                        if (parameters.Length == 0)
                        {
                            _entryPoint.Invoke(null, []);
                        }
                        else
                        {
                            _entryPoint.Invoke(null, new object[] { _args });
                        }

                        // Try to set an exception if the entry point returns gracefully, this will force
                        // build to throw
                        _hostTcs.TrySetException(new InvalidOperationException("The entry point exited without ever building an IHost."));
                    }
                    catch (TargetInvocationException tie) when (tie.InnerException.GetType().Name == "HostAbortedException")
                    {
                        // The host was stopped by our own logic
                    }
                    catch (TargetInvocationException tie)
                    {
                        exception = tie.InnerException ?? tie;

                        // Another exception happened, propagate that to the caller
                        _hostTcs.TrySetException(exception);
                    }
                    catch (Exception ex)
                    {
                        exception = ex;

                        // Another exception happened, propagate that to the caller
                        _hostTcs.TrySetException(ex);
                    }
                    finally
                    {
                        // Signal that the entry point is completed
                        _entrypointCompleted.Invoke(exception);
                    }
                })
                {
                    // Make sure this doesn't hang the process
                    IsBackground = true
                };

                // Start the thread
                thread.Start();

                try
                {
                    // Wait before throwing an exception
                    if (!_hostTcs.Task.Wait(_waitTimeout))
                    {
                        throw new InvalidOperationException($"Timed out waiting for the entry point to build the IHost after {s_defaultWaitTimeout}. This timeout can be modified using the '{TimeoutEnvironmentKey}' environment variable.");
                    }
                }
                catch (AggregateException) when (_hostTcs.Task.IsCompleted)
                {
                    // Lets this propagate out of the call to GetAwaiter().GetResult()
                }

                Debug.Assert(_hostTcs.Task.IsCompleted);

                return _hostTcs.Task.GetAwaiter().GetResult();
            }

            public void OnCompleted()
            {
                _disposable.Dispose();
            }

            public void OnError(Exception error)
            {

            }

            public void OnNext(DiagnosticListener value)
            {
                if (_currentListener.Value != this)
                {
                    // Ignore events that aren't for this listener
                    return;
                }

                if (value.Name == "Microsoft.Extensions.Hosting")
                {
                    _disposable = value.Subscribe(this);
                }
            }

            public void OnNext(KeyValuePair<string, object> value)
            {
                if (_currentListener.Value != this)
                {
                    // Ignore events that aren't for this listener
                    return;
                }

                if (value.Key == "HostBuilding")
                {
                    _configure.Invoke(value.Value!);
                }

                if (value.Key == "HostBuilt")
                {
                    _hostTcs.TrySetResult(value.Value!);

                    if (_stopApplication)
                    {
                        // Stop the host from running further
                        ThrowHostAborted();
                    }
                }
            }

            // HostFactoryResolver is used by tools that explicitly don't want to reference Microsoft.Extensions.Hosting assemblies.
            // So don't depend on the public HostAbortedException directly. Instead, load the exception type dynamically if it can
            // be found. If it can't (possibly because the app is using an older version), throw a private exception with the same name.
            private static void ThrowHostAborted()
            {
                var publicHostAbortedExceptionType = Type.GetType("Microsoft.Extensions.Hosting.HostAbortedException, Microsoft.Extensions.Hosting.Abstractions", throwOnError: false);
                if (publicHostAbortedExceptionType != null)
                {
                    throw (Exception)Activator.CreateInstance(publicHostAbortedExceptionType)!;
                }
                else
                {
                    throw new HostAbortedException();
                }
            }

            private sealed class HostAbortedException : Exception
            {
            }
        }
    }

    private class TestAdditionalText(string path, string text) : AdditionalText
    {
        public override string Path => path;

        public override SourceText GetText(CancellationToken cancellationToken = default)
        {
            return SourceText.From(text, Encoding.UTF8);
        }
    }
}
