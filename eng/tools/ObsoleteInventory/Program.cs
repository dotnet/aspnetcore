// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ObsoleteInventory;

class Program
{
    static int Main(string[] args)
    {
        var repoRoot = FindRepoRoot();
        if (repoRoot is null)
        {
            Console.Error.WriteLine("Error: Could not find repository root.");
            return 1;
        }

        var verify = args.Length > 0 && args[0] == "--verify";
        var outputPath = Path.Combine(repoRoot, "ObsoletedApis.md");

        Console.WriteLine($"Repository root: {repoRoot}");
        Console.WriteLine($"Output: {outputPath}");
        Console.WriteLine();

        var srcDir = Path.Combine(repoRoot, "src");
        if (!Directory.Exists(srcDir))
        {
            Console.Error.WriteLine($"Error: src directory not found at {srcDir}");
            return 1;
        }

        // Collect all obsolete APIs
        var records = CollectObsoleteApis(repoRoot, srcDir);

        // Generate markdown content
        var markdown = GenerateMarkdown(records, repoRoot);

        if (verify)
        {
            // Verify mode: check if the file matches
            if (!File.Exists(outputPath))
            {
                Console.Error.WriteLine($"Error: File {outputPath} does not exist.");
                return 1;
            }

            var existingContent = File.ReadAllText(outputPath);
            if (existingContent != markdown)
            {
                Console.Error.WriteLine("Error: ObsoletedApis.md is out of date. Run without --verify to regenerate.");
                return 1;
            }

            Console.WriteLine("ObsoletedApis.md is up to date.");
            return 0;
        }
        else
        {
            // Write the file
            File.WriteAllText(outputPath, markdown);
            Console.WriteLine($"Generated {outputPath}");
            Console.WriteLine($"Total obsolete APIs: {records.Count}");
            return 0;
        }
    }

    static string? FindRepoRoot()
    {
        var current = Directory.GetCurrentDirectory();
        while (current is not null)
        {
            if (Directory.Exists(Path.Combine(current, ".git")))
            {
                return current;
            }
            current = Directory.GetParent(current)?.FullName;
        }
        return null;
    }

    static List<ObsoleteApiRecord> CollectObsoleteApis(string repoRoot, string srcDir)
    {
        var records = new List<ObsoleteApiRecord>();
        var files = Directory.EnumerateFiles(srcDir, "*.cs", SearchOption.AllDirectories);

        var blameCache = new Dictionary<string, Dictionary<int, (string Commit, DateTime Date)>>();
        var commitDateCache = new Dictionary<string, DateTime>();

        foreach (var file in files)
        {
            // Skip unwanted paths
            var relativePath = Path.GetRelativePath(repoRoot, file).Replace('\\', '/');
            if (ShouldSkipFile(relativePath))
            {
                continue;
            }

            // Check for auto-generated header in first 5 non-empty lines
            if (IsGeneratedFile(file))
            {
                continue;
            }

            // Parse the file
            var sourceText = File.ReadAllText(file);
            var syntaxTree = CSharpSyntaxTree.ParseText(sourceText, path: file);
            var root = syntaxTree.GetRoot();

            // Find all declarations with Obsolete attributes
            var declarations = root.DescendantNodes()
                .Where(n => n is MemberDeclarationSyntax || n is BaseTypeDeclarationSyntax)
                .Cast<CSharpSyntaxNode>();

            foreach (var decl in declarations)
            {
                var obsoleteAttr = GetObsoleteAttribute(decl);
                if (obsoleteAttr is not null)
                {
                    var lineNumber = syntaxTree.GetLineSpan(obsoleteAttr.Span).StartLinePosition.Line + 1;
                    
                    // Get blame info for this line
                    if (!blameCache.TryGetValue(file, out var fileBlame))
                    {
                        fileBlame = GetBlameInfo(repoRoot, file, commitDateCache);
                        blameCache[file] = fileBlame;
                    }

                    if (!fileBlame.TryGetValue(lineNumber, out var blameInfo))
                    {
                        Console.WriteLine($"Warning: Could not get blame info for {relativePath}:{lineNumber}");
                        continue;
                    }

                    var (message, isError) = ParseObsoleteAttribute(obsoleteAttr);
                    var symbol = GetSymbolInfo(decl);

                    var record = new ObsoleteApiRecord
                    {
                        Api = symbol.FullSignature,
                        Kind = symbol.Kind,
                        IntroducedObsoleteDate = blameInfo.Date,
                        Commit = blameInfo.Commit,
                        IsError = isError,
                        Message = message,
                        File = relativePath
                    };

                    records.Add(record);
                }
            }
        }

        // Sort by date (oldest first)
        records.Sort((a, b) => a.IntroducedObsoleteDate.CompareTo(b.IntroducedObsoleteDate));

        return records;
    }

    static bool ShouldSkipFile(string relativePath)
    {
        var lower = relativePath.ToLowerInvariant();
        
        // Skip test/benchmark/sample directories
        if (lower.Contains("/test/") || lower.Contains("/tests/") || 
            lower.Contains("/benchmark/") || lower.Contains("/benchmarks/") ||
            lower.Contains("/sample/") || lower.Contains("/samples/"))
        {
            return true;
        }

        // Skip obj/bin directories
        if (lower.Contains("/obj/") || lower.Contains("/bin/"))
        {
            return true;
        }

        // Skip eng/tools directories
        if (lower.StartsWith("eng/", StringComparison.Ordinal) || lower.StartsWith("tools/", StringComparison.Ordinal))
        {
            return true;
        }

        return false;
    }

    static bool IsGeneratedFile(string filePath)
    {
        try
        {
            using var reader = new StreamReader(filePath);
            var nonEmptyLines = 0;
            string? line;
            
            while ((line = reader.ReadLine()) is not null && nonEmptyLines < 5)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed))
                {
                    continue;
                }

                nonEmptyLines++;

                // Check for common auto-generated markers
                if (trimmed.Contains("<auto-generated>") || 
                    trimmed.Contains("auto-generated") ||
                    trimmed.Contains("Generated by") ||
                    trimmed.Contains("This code was generated"))
                {
                    return true;
                }
            }
        }
        catch
        {
            // If we can't read the file, don't skip it
        }

        return false;
    }

    static AttributeSyntax? GetObsoleteAttribute(CSharpSyntaxNode node)
    {
        var attributeLists = node switch
        {
            BaseTypeDeclarationSyntax typeDecl => typeDecl.AttributeLists,
            MethodDeclarationSyntax method => method.AttributeLists,
            PropertyDeclarationSyntax property => property.AttributeLists,
            FieldDeclarationSyntax field => field.AttributeLists,
            EventDeclarationSyntax eventDecl => eventDecl.AttributeLists,
            EnumMemberDeclarationSyntax enumMember => enumMember.AttributeLists,
            ConstructorDeclarationSyntax ctor => ctor.AttributeLists,
            OperatorDeclarationSyntax op => op.AttributeLists,
            ConversionOperatorDeclarationSyntax conversion => conversion.AttributeLists,
            IndexerDeclarationSyntax indexer => indexer.AttributeLists,
            DelegateDeclarationSyntax delegateDecl => delegateDecl.AttributeLists,
            _ => default(SyntaxList<AttributeListSyntax>)
        };

        foreach (var attrList in attributeLists)
        {
            foreach (var attr in attrList.Attributes)
            {
                var name = attr.Name.ToString();
                if (name.Equals("Obsolete", StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("ObsoleteAttribute", StringComparison.OrdinalIgnoreCase))
                {
                    return attr;
                }
            }
        }

        return null;
    }

    static (string Message, bool IsError) ParseObsoleteAttribute(AttributeSyntax attr)
    {
        var message = "";
        var isError = false;

        if (attr.ArgumentList is not null)
        {
            var args = attr.ArgumentList.Arguments;
            if (args.Count > 0)
            {
                var firstArg = args[0].Expression;
                if (firstArg is LiteralExpressionSyntax literal && literal.Token.Value is string str)
                {
                    message = str;
                }
                else if (firstArg is MemberAccessExpressionSyntax)
                {
                    // Handle constant references like Obsoletions.SomeMessage
                    message = firstArg.ToString();
                }
            }

            if (args.Count > 1)
            {
                var secondArg = args[1].Expression;
                if (secondArg is LiteralExpressionSyntax literal && literal.Token.Value is bool boolValue)
                {
                    isError = boolValue;
                }
            }
        }

        return (message, isError);
    }

    static SymbolInfo GetSymbolInfo(CSharpSyntaxNode node)
    {
        var kind = node switch
        {
            ClassDeclarationSyntax => "Type",
            StructDeclarationSyntax => "Type",
            InterfaceDeclarationSyntax => "Type",
            EnumDeclarationSyntax => "Type",
            RecordDeclarationSyntax => "Type",
            DelegateDeclarationSyntax => "Type",
            MethodDeclarationSyntax => "Method",
            PropertyDeclarationSyntax => "Property",
            FieldDeclarationSyntax => "Field",
            EventDeclarationSyntax => "Event",
            EnumMemberDeclarationSyntax => "EnumMember",
            ConstructorDeclarationSyntax => "Constructor",
            OperatorDeclarationSyntax => "Operator",
            ConversionOperatorDeclarationSyntax => "Operator",
            IndexerDeclarationSyntax => "Property",
            _ => "Unknown"
        };

        var signature = BuildSignature(node);

        return new SymbolInfo(signature, kind);
    }

    static string BuildSignature(CSharpSyntaxNode node)
    {
        var parts = new List<string>();

        // Get namespace
        var namespaceDecl = node.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault();
        if (namespaceDecl is not null)
        {
            parts.Add(namespaceDecl.Name.ToString());
        }

        // Get containing types
        var containingTypes = node.Ancestors().OfType<BaseTypeDeclarationSyntax>().Reverse();
        foreach (var type in containingTypes)
        {
            parts.Add(GetTypeName(type));
        }

        // Add the member itself
        parts.Add(GetMemberSignature(node));

        return string.Join(".", parts);
    }

    static string GetTypeName(BaseTypeDeclarationSyntax type)
    {
        var name = type.Identifier.Text;
        
        // Add generic parameters if any
        if (type is TypeDeclarationSyntax typeDecl && typeDecl.TypeParameterList is not null)
        {
            var typeParams = string.Join(", ", typeDecl.TypeParameterList.Parameters.Select(p => p.Identifier.Text));
            name += $"<{typeParams}>";
        }

        return name;
    }

    static string GetMemberSignature(CSharpSyntaxNode node)
    {
        switch (node)
        {
            case ClassDeclarationSyntax classDecl:
                return GetTypeName(classDecl);
            
            case StructDeclarationSyntax structDecl:
                return GetTypeName(structDecl);
            
            case InterfaceDeclarationSyntax interfaceDecl:
                return GetTypeName(interfaceDecl);
            
            case EnumDeclarationSyntax enumDecl:
                return enumDecl.Identifier.Text;
            
            case RecordDeclarationSyntax recordDecl:
                return GetTypeName(recordDecl);
            
            case DelegateDeclarationSyntax delegateDecl:
                var delegateParams = delegateDecl.ParameterList is not null 
                    ? $"({string.Join(", ", delegateDecl.ParameterList.Parameters.Select(p => $"{p.Type} {p.Identifier}"))})"
                    : "()";
                return $"{delegateDecl.Identifier.Text}{delegateParams}";
            
            case MethodDeclarationSyntax method:
                var methodParams = method.ParameterList is not null
                    ? $"({string.Join(", ", method.ParameterList.Parameters.Select(p => $"{p.Type} {p.Identifier}"))})"
                    : "()";
                var typeParams = method.TypeParameterList is not null
                    ? $"<{string.Join(", ", method.TypeParameterList.Parameters.Select(p => p.Identifier.Text))}>"
                    : "";
                return $"{method.Identifier.Text}{typeParams}{methodParams}";
            
            case PropertyDeclarationSyntax property:
                return property.Identifier.Text;
            
            case FieldDeclarationSyntax field:
                // Fields can have multiple variables
                var variables = field.Declaration.Variables.Select(v => v.Identifier.Text);
                return string.Join(", ", variables);
            
            case EventDeclarationSyntax eventDecl:
                return eventDecl.Identifier.Text;
            
            case EnumMemberDeclarationSyntax enumMember:
                return enumMember.Identifier.Text;
            
            case ConstructorDeclarationSyntax ctor:
                var ctorParams = ctor.ParameterList is not null
                    ? $"({string.Join(", ", ctor.ParameterList.Parameters.Select(p => $"{p.Type} {p.Identifier}"))})"
                    : "()";
                return $"{ctor.Identifier.Text}{ctorParams}";
            
            case OperatorDeclarationSyntax op:
                var opParams = op.ParameterList is not null
                    ? $"({string.Join(", ", op.ParameterList.Parameters.Select(p => $"{p.Type} {p.Identifier}"))})"
                    : "()";
                return $"operator {op.OperatorToken.Text}{opParams}";
            
            case ConversionOperatorDeclarationSyntax conversion:
                var convParams = conversion.ParameterList is not null
                    ? $"({string.Join(", ", conversion.ParameterList.Parameters.Select(p => $"{p.Type} {p.Identifier}"))})"
                    : "()";
                return $"{conversion.ImplicitOrExplicitKeyword.Text} operator {conversion.Type}{convParams}";
            
            case IndexerDeclarationSyntax indexer:
                var indexerParams = indexer.ParameterList is not null
                    ? $"[{string.Join(", ", indexer.ParameterList.Parameters.Select(p => $"{p.Type} {p.Identifier}"))}]"
                    : "[]";
                return $"this{indexerParams}";
            
            default:
                return node.ToString().Split('\n')[0].Trim();
        }
    }

    static Dictionary<int, (string Commit, DateTime Date)> GetBlameInfo(
        string repoRoot, 
        string filePath, 
        Dictionary<string, DateTime> commitDateCache)
    {
        var result = new Dictionary<int, (string, DateTime)>();
        var relativePath = Path.GetRelativePath(repoRoot, filePath);

        var psi = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = $"blame --line-porcelain \"{relativePath}\"",
            WorkingDirectory = repoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);
        if (process is null)
        {
            return result;
        }

        string? currentCommit = null;
        int? currentLineNumber = null;

        while (!process.StandardOutput.EndOfStream)
        {
            var line = process.StandardOutput.ReadLine();
            if (line is null)
            {
                break;
            }

            if (line.Length >= 40 && Regex.IsMatch(line.Substring(0, 40), "^[0-9a-f]{40}"))
            {
                // This is a commit line
                var parts = line.Split(' ');
                currentCommit = parts[0];
                currentLineNumber = int.Parse(parts[2], CultureInfo.InvariantCulture);
            }
            else if (line.StartsWith("committer-time ", StringComparison.Ordinal) && currentCommit is not null && currentLineNumber.HasValue)
            {
                var timestamp = long.Parse(line.Substring("committer-time ".Length), CultureInfo.InvariantCulture);
                var date = DateTimeOffset.FromUnixTimeSeconds(timestamp).UtcDateTime;
                
                result[currentLineNumber.Value] = (currentCommit.Substring(0, 7), date);
            }
        }

        process.WaitForExit();
        return result;
    }

    static string GenerateMarkdown(List<ObsoleteApiRecord> records, string repoRoot)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("# Obsoleted APIs Report");
        sb.AppendLine();
        sb.AppendLine("This file contains an inventory of all APIs currently marked with the `[Obsolete]` attribute in the repository.");
        sb.AppendLine();
        sb.AppendLine("| API | Kind | IntroducedObsoleteDate | Commit | IsError | Message | File |");
        sb.AppendLine("|-----|------|------------------------|--------|---------|---------|------|");

        foreach (var record in records)
        {
            var api = $"`{record.Api}`";
            var date = record.IntroducedObsoleteDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var commit = $"[{record.Commit}](https://github.com/dotnet/aspnetcore/commit/{record.Commit})";
            var message = EscapeMarkdown(TruncateMessage(record.Message));
            
            sb.AppendLine(CultureInfo.InvariantCulture, $"| {api} | {record.Kind} | {date} | {commit} | {record.IsError} | {message} | {record.File} |");
        }

        sb.AppendLine();
        sb.AppendLine(CultureInfo.InvariantCulture, $"**Total:** {records.Count} obsolete APIs");

        return sb.ToString();
    }

    static string EscapeMarkdown(string text)
    {
        return text.Replace("|", "\\|");
    }

    static string TruncateMessage(string message)
    {
        const int maxLength = 240;
        if (message.Length <= maxLength)
        {
            return message;
        }
        return message.Substring(0, maxLength) + "...";
    }

    record SymbolInfo(string FullSignature, string Kind);

    record ObsoleteApiRecord
    {
        public required string Api { get; init; }
        public required string Kind { get; init; }
        public required DateTime IntroducedObsoleteDate { get; init; }
        public required string Commit { get; init; }
        public required bool IsError { get; init; }
        public required string Message { get; init; }
        public required string File { get; init; }
    }
}
