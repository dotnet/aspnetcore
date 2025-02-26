// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.OpenApi.SourceGenerators;

[Generator]
public sealed partial class XmlCommentGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Pull out XML comments from referenced assemblies passed in as AdditionalFiles.
        var commentsFromXmlFile = context.AdditionalTextsProvider
            .Where(file => file.Path.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            .Select(ParseXmlFile);
        // Pull out XML comments from the target assembly using the information produced
        // by Roslyn into the compilation.
        var commentsFromTargetAssembly = context.CompilationProvider
            .Select(ParseCompilation);
        // Map string XML comments to structured data from both the AdditionalFiles
        // and the target assembly.
        var parsedCommentsFromXmlFile = commentsFromXmlFile
            .Combine(context.CompilationProvider)
            .Select(ParseComments);
        var parsedCommentsFromCompilation = commentsFromTargetAssembly
            .Combine(context.CompilationProvider)
            .Select(ParseComments);
        // Discover AddOpenApi invocations so that we can intercept them with an implicit
        // registration of the transformers for mapping XML doc comments to the OpenAPI file.
        var groupedAddOpenApiInvocations = context.SyntaxProvider
            .CreateSyntaxProvider(FilterInvocations, GetAddOpenApiOverloadVariant)
            .GroupWith((variantDetails) => variantDetails.Location, AddOpenApiInvocationComparer.Instance)
            .Collect();

        var generatedCommentsFromXmlFile = parsedCommentsFromXmlFile
            .Select(EmitCommentsCache);
        var generatedCommentsFromCompilation = parsedCommentsFromCompilation
            .Select(EmitCommentsCache);

        var result = generatedCommentsFromXmlFile.Collect()
            .Combine(generatedCommentsFromCompilation)
            .Combine(groupedAddOpenApiInvocations);

        context.RegisterSourceOutput(result, (context, output) =>
        {
            var groupedAddOpenApiInvocations = output.Right;
            var (generatedCommentsFromXmlFile, generatedCommentsFromCompilation) = output.Left;
            var compiledXmlFileComments = !generatedCommentsFromXmlFile.IsDefaultOrEmpty
                ? string.Join("\n", generatedCommentsFromXmlFile)
                : string.Empty;
            Emit(context, compiledXmlFileComments, generatedCommentsFromCompilation, groupedAddOpenApiInvocations);
        });
    }
}
