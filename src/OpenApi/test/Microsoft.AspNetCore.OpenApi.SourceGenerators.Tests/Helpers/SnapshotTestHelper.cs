// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Loader;
using System.Text;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

namespace Microsoft.AspNetCore.OpenApi.SourceGenerators.Tests;

[UsesVerify]
public static class SnapshotTestHelper
{
    private static readonly CSharpParseOptions ParseOptions = new CSharpParseOptions(LanguageVersion.Preview)
        .WithFeatures([new KeyValuePair<string, string>("InterceptorsNamespaces", "Microsoft.AspNetCore.OpenApi.Generated")]);

    public static Task Verify(string source, IIncrementalGenerator generator, out Compilation compilation)
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
                ]);
        var inputCompilation = CSharpCompilation.Create("OpenApiXmlCommentGeneratorSample",
            [CSharpSyntaxTree.ParseText(source, options: ParseOptions, path: "Program.cs")],
            references,
            new CSharpCompilationOptions(OutputKind.ConsoleApplication));
        var driver = CSharpGeneratorDriver.Create(generators: [generator.AsSourceGenerator()], parseOptions: ParseOptions);
        return Verifier
            .Verify(driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out compilation, out var diagnostics))
            .AutoVerify()
            .UseDirectory("../snapshots");
    }

    public static async Task VerifyOpenApi(Compilation compilation, Action<OpenApiDocument> verifyFunc)
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

            var service = services.GetService(serviceType) ?? throw new InvalidOperationException("Could not resolve IDocumntProvider service.");
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
        public Task StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken) where TContext: notnull => Task.CompletedTask;
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

}
