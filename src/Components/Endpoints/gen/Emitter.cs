// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.AspNetCore.Components.Endpoints.Generator;
internal class Emitter
{
    internal static string GetComponentsBody(ComponentModel cm)
    {
        var typeofExpression = GetTypeSymbolTypeofExpression(cm);

        var builder = new StringBuilder();
        var writer = new StringWriter(builder);
        var codeWriter = new CodeWriter(writer, 3);
        codeWriter.WriteLine($"new {ComponentEndpointsGeneratorSources.ComponentBuilder}");
        codeWriter.StartBlock();
        codeWriter.WriteLine($"AssemblyName = source,");
        codeWriter.WriteLine($"ComponentType = typeof({typeofExpression}),");
        if (cm.RenderMode != null)
        {
            codeWriter.Write($"RenderMode = ");
            EmitAttributeInstance(codeWriter, cm.RenderMode);
            codeWriter.WriteLine();
        }
        codeWriter.EndBlockWithComma(newLine: false);
        codeWriter.Flush();
        writer.Flush();
        return builder.ToString();
    }

    // Generates the value that goes inside a typeof expression for a given symbol.
    // For example, for IEnumerable<T> it would generate "global::System.Collections.Generic.IEnumerable<>"
    // For KeyValuePair<TKey, TValue> it would generate "global::System.Collections.Generic.KeyValuePair<,>"
    private static string GetTypeSymbolTypeofExpression(ComponentModel cm)
    {
        if (cm.Component.IsGenericType)
        {
            return cm.Component.ConstructUnboundGenericType().ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        }

        return cm.Component.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }

    private static void EmitAttributeInstance(CodeWriter codeWriter, AttributeData renderMode)
    {
        codeWriter.Write($"new {renderMode.AttributeClass!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}");
        if (renderMode.ConstructorArguments.Length > 0)
        {
            codeWriter.StartParameterListBlock();
            for (var i = 0; i < renderMode.ConstructorArguments.Length - 1; i++)
            {
                var argument = renderMode.ConstructorArguments[i];
                codeWriter.Write(argument.ToCSharpString());
                codeWriter.WriteLine(",");
            }

            var lastArgument = renderMode.ConstructorArguments[renderMode.ConstructorArguments.Length - 1];
            codeWriter.Write(lastArgument.ToCSharpString());
            codeWriter.EndParameterListBlock();
        }
        if (renderMode.NamedArguments.Length > 0)
        {
            codeWriter.StartBlock();
            for (var i = 0; i < renderMode.NamedArguments.Length - 1; i++)
            {
                var argument = renderMode.NamedArguments[i];
                codeWriter.Write(argument.Key);
                codeWriter.Write(" = ");
                codeWriter.Write(argument.Value.ToCSharpString());
                codeWriter.WriteLine(",");
            }

            var lastArgument = renderMode.NamedArguments[renderMode.ConstructorArguments.Length - 1];
            codeWriter.Write(lastArgument.Key);
            codeWriter.Write(" = ");
            codeWriter.Write(lastArgument.Value.ToCSharpString());
            codeWriter.EndBlock();
        }
    }

    internal static string CreateGetComponentsMethodSignature(IAssemblySymbol assembly)
    {
        var name = assembly.Name.Replace(".", "_");
        var builder = new StringBuilder();
        var writer = new StringWriter(builder);
        var codeWriter = new CodeWriter(writer, 1);
        var returnType = $"{ComponentEndpointsGeneratorSources.ComponentBuilder} []";
        codeWriter.Write($"private {returnType} Get{name}Components(string source)");
        codeWriter.Flush();
        writer.Flush();
        return builder.ToString();
    }

    internal static string CreateGetMethod(string signature, ImmutableArray<string> body, string typeName)
    {
        var builder = new StringBuilder();
        var writer = new StringWriter(builder);
        var codeWriter = new CodeWriter(writer, 1);
        codeWriter.WriteLine(signature);
        codeWriter.StartBlock();

        if (body.Length == 0)
        {
            codeWriter.WriteLine($"return global::System.Array.Empty<{typeName}>();");
        }
        else
        {
            codeWriter.WriteLine($"return new {typeName}[]");
            codeWriter.StartBlock();
            for (var i = 0; i < body.Length; i++)
            {
                var definition = body[i];
                codeWriter.WriteLine(definition);
            }
            codeWriter.EndBlockWithSemiColon(newLine: true);
        }

        codeWriter.EndBlock();
        codeWriter.Flush();
        writer.Flush();
        return builder.ToString();
    }

    internal static string GetPagesBody(ComponentModel cm)
    {
        //        new PageComponentBuilder()
        //        {
        //            Source = "Blazor.United.Assembly",
        //            PageType = typeof(Counter),
        //            RouteTemplates = new List<string> { "/counter" }
        //        }
        var typeofExpression = GetTypeSymbolTypeofExpression(cm);

        var builder = new StringBuilder();
        var writer = new StringWriter(builder);
        var codeWriter = new CodeWriter(writer, 3);
        codeWriter.WriteLine($"new {ComponentEndpointsGeneratorSources.PageComponentBuilder}");
        codeWriter.StartBlock();
        codeWriter.WriteLine($"AssemblyName = source,");
        codeWriter.WriteLine($"PageType = typeof({typeofExpression}),");
        codeWriter.WriteLine($$"""RouteTemplates = new global::System.Collections.Generic.List<string>""");
        codeWriter.StartBlock();
        foreach (var route in cm.Routes)
        {
            codeWriter.Write(route.ConstructorArguments[0].ToCSharpString());
            codeWriter.WriteLine(",");
        }
        codeWriter.EndBlock();
        codeWriter.EndBlockWithComma(newLine: false);
        codeWriter.Flush();
        writer.Flush();
        return builder.ToString();
    }

    internal static string CreateGetPagesMethodSignature(IAssemblySymbol assembly)
    {
        var name = assembly.Name.Replace(".", "_");
        var builder = new StringBuilder();
        var writer = new StringWriter(builder);
        var codeWriter = new CodeWriter(writer, 1);
        var returnType = $"{ComponentEndpointsGeneratorSources.PageComponentBuilder} []";
        codeWriter.Write($"private {returnType} Get{name}Pages(string source)");
        codeWriter.Flush();
        writer.Flush();
        return builder.ToString();
    }

    internal static string CreateGetLibraryMethodThunk(IAssemblySymbol assembly)
    {
        var name = assembly.Name.Replace(".", "_");
        var builder = new StringBuilder();
        var writer = new StringWriter(builder);
        var codeWriter = new CodeWriter(writer, 1);
        var returnType = $"{ComponentEndpointsGeneratorSources.AssemblyComponentLibraryDescriptor}";
        codeWriter.WriteLine($"private {returnType} Get{name}Builder()");
        codeWriter.StartBlock();
        codeWriter.WriteLine($"var source = \"{assembly.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}\";");
        codeWriter.Write($"return new {returnType}");
        codeWriter.StartParameterListBlock();
        codeWriter.WriteLine("source,");
        codeWriter.WriteLine($"Get{name}Pages(source),");
        codeWriter.Write($"Get{name}Components(source)");
        codeWriter.EndParameterListBlock();
        codeWriter.WriteLine(";");
        codeWriter.EndBlock();
        codeWriter.Flush();
        writer.Flush();
        return builder.ToString();
    }

    internal static string CreateGetBuilderThunk(ImmutableArray<string> getLibraryThunks)
    {
        var builder = new StringBuilder();
        var writer = new StringWriter(builder);
        var codeWriter = new CodeWriter(writer, 1);
        codeWriter.WriteLine("public override global::Microsoft.AspNetCore.Components.Discovery.ComponentApplicationBuilder GetBuilder()");
        codeWriter.StartBlock();
        codeWriter.WriteLine("var builder = new global::Microsoft.AspNetCore.Components.Discovery.ComponentApplicationBuilder();");
        for (var i = 0; i < getLibraryThunks.Length; i++)
        {
            codeWriter.WriteLine(getLibraryThunks[i]);
        }
        codeWriter.WriteLine("return builder;");
        codeWriter.EndBlock();
        codeWriter.Flush();
        writer.Flush();
        return builder.ToString();
    }

    internal static string CreateLibraryThunk(IAssemblySymbol assembly)
    {
        var name = assembly.Name.Replace(".", "_");
        return $"builder.AddLibrary(Get{name}Builder());";
    }
}
