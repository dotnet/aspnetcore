// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

internal static class SyntaxTreeExtensions
{
    // Utilize the same logic used by the `CallerLinePathAttribute` for generating
    // a file path for a given syntax tree.
    // Source copied from https://github.com/dotnet/roslyn/blob/5b47c7fe326faa35940f220c14f718cd0b820c38/src/Compilers/Core/Portable/Syntax/SyntaxTree.cs#L274-L293 until
    // public APIs are available.
    internal static string GetDisplayPath(this SyntaxTree tree, TextSpan span, SourceReferenceResolver? resolver)
    {
        var mappedSpan = tree.GetMappedLineSpan(span);
        if (resolver == null || mappedSpan.Path.Length == 0)
        {
            return mappedSpan.Path;
        }

        return resolver.NormalizePath(mappedSpan.Path, baseFilePath: mappedSpan.HasMappedPath ? tree.FilePath : null) ?? mappedSpan.Path;
    }
}
