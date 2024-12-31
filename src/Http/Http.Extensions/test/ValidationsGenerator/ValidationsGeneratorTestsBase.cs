// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using System.Runtime.Loader;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.ValidationsGenerator;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

[UsesVerify]
public class ValidationsGeneratorTestsBase
{
    private static readonly CSharpParseOptions _parseOptions = new CSharpParseOptions(LanguageVersion.Preview)
        .WithFeatures([new KeyValuePair<string, string>("InterceptorsNamespaces", "Microsoft.AspNetCore.Http.Validations.Generated")]);

    private static string CreateSourceText(string source) => $$"""
{{source}}
// Make Program class public for consumption
// in WebApplicationFactory
public partial class Program { }
""";

    public Task Verify(string source, out Compilation compilation)
    {
        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => !assembly.IsDynamic && !string.IsNullOrWhiteSpace(assembly.Location))
            .Select(assembly => MetadataReference.CreateFromFile(assembly.Location))
            .Concat(
            [
                MetadataReference.CreateFromFile(typeof(WebApplicationBuilder).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(EndpointRouteBuilderExtensions).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(IApplicationBuilder).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Microsoft.AspNetCore.Mvc.ApiExplorer.IApiDescriptionProvider).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Microsoft.AspNetCore.Mvc.ControllerBase).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(MvcCoreMvcBuilderExtensions).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(TypedResults).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Text.Json.Nodes.JsonArray).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Uri).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.ComponentModel.DataAnnotations.ValidationAttribute).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(RouteData).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(IFeatureCollection).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(ValidateOptionsResult).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(IHttpMethodMetadata).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(IResult).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(HttpJsonServiceExtensions).Assembly.Location),
            ]);
        var generator = new ValidationsGenerator();
        var inputCompilation = CSharpCompilation.Create($"ValidationsGeneratorSample-{Guid.NewGuid()}",
            [CSharpSyntaxTree.ParseText(CreateSourceText(source), options: _parseOptions, path: "Program.cs")],
            references,
            new CSharpCompilationOptions(OutputKind.ConsoleApplication, nullableContextOptions: NullableContextOptions.Enable));
        var driver = CSharpGeneratorDriver.Create(generators: [generator.AsSourceGenerator()], parseOptions: _parseOptions);
        return Verifier
            .Verify(driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out compilation, out _))
            .AutoVerify()
            .UseDirectory("snapshots");
    }

    public async Task VerifyEndpoint(Compilation compilation, Func<HttpClient, Task> verifyEndpoint)
    {
        var symbolsName = compilation.AssemblyName;
        var output = new MemoryStream();
        var pdb = new MemoryStream();

        var emitOptions = new EmitOptions(debugInformationFormat: DebugInformationFormat.PortablePdb, pdbFilePath: symbolsName);

        var embeddedTexts = new List<EmbeddedText>();

        // Make sure we embed the sources in pdb for easy debugging
        foreach (var syntaxTree in compilation.SyntaxTrees)
        {
            var text = syntaxTree.GetText();
            var encoding = text.Encoding ?? Encoding.UTF8;
            var buffer = encoding.GetBytes(text.ToString());
            var sourceText = SourceText.From(buffer, buffer.Length, encoding, canBeEmbedded: true);

            var syntaxRootNode = (CSharpSyntaxNode)syntaxTree.GetRoot();
            var newSyntaxTree = CSharpSyntaxTree.Create(syntaxRootNode, options: _parseOptions, encoding: encoding, path: syntaxTree.FilePath);

            compilation = compilation.ReplaceSyntaxTree(syntaxTree, newSyntaxTree);

            embeddedTexts.Add(EmbeddedText.FromSource(syntaxTree.FilePath, sourceText));
        }

        _ = compilation.Emit(output, pdb, options: emitOptions, embeddedTexts: embeddedTexts);

        output.Position = 0;
        pdb.Position = 0;

        var assembly = AssemblyLoadContext.Default.LoadFromStream(output, pdb);

        var depsFileName = $"{assembly.GetName().Name}.deps.json";
        var depsFile = new FileInfo(Path.Combine(AppContext.BaseDirectory, depsFileName));
        File.Create(depsFile.FullName).Dispose();

        var factory = Activator.CreateInstance(typeof(WebApplicationFactory<>).MakeGenericType(assembly?.GetType("Program")!));
        var client = (HttpClient)factory.GetType().GetMethod("CreateClient", Type.EmptyTypes).Invoke(factory, null);
        await verifyEndpoint(client);
    }
}
