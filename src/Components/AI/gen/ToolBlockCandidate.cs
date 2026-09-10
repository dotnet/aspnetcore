// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Components.AI.SourceGenerators;

// The types below flow through the incremental generator pipeline, so they must be
// value-equatable to let Roslyn cache correctly. They are implemented as immutable
// classes with manual equality (rather than records) and use sequence equality for
// their collections.
internal sealed class ToolBlockCandidate : IEquatable<ToolBlockCandidate>
{
    public ToolBlockCandidate(
        string @namespace,
        string className,
        string blockTypeGlobal,
        string toolName,
        IReadOnlyList<ToolParameterInfo> parameters,
        IReadOnlyList<ToolResultPropertyInfo> resultProperties)
    {
        Namespace = @namespace;
        ClassName = className;
        BlockTypeGlobal = blockTypeGlobal;
        ToolName = toolName;
        Parameters = parameters;
        ResultProperties = resultProperties;
    }

    // Escaped namespace suitable for emitting (keyword segments carry an '@' prefix).
    public string Namespace { get; }

    // Simple metadata name of the annotated type, used for the handler name and hint name.
    public string ClassName { get; }

    // Fully-qualified, keyword-escaped reference (e.g. global::App.WeatherBlock) used
    // wherever the block type is referenced in generated code.
    public string BlockTypeGlobal { get; }

    public string ToolName { get; }

    public IReadOnlyList<ToolParameterInfo> Parameters { get; }

    public IReadOnlyList<ToolResultPropertyInfo> ResultProperties { get; }

    public bool Equals(ToolBlockCandidate? other)
    {
        if (other is null)
        {
            return false;
        }

        return Namespace == other.Namespace
            && ClassName == other.ClassName
            && BlockTypeGlobal == other.BlockTypeGlobal
            && ToolName == other.ToolName
            && Parameters.SequenceEqual(other.Parameters)
            && ResultProperties.SequenceEqual(other.ResultProperties);
    }

    public override bool Equals(object? obj)
        => Equals(obj as ToolBlockCandidate);

    public override int GetHashCode()
    {
        var hash = Hash.Combine(Namespace, ClassName);
        hash = Hash.Combine(hash, BlockTypeGlobal.GetHashCode());
        hash = Hash.Combine(hash, ToolName.GetHashCode());
        foreach (var p in Parameters)
        {
            hash = Hash.Combine(hash, p.GetHashCode());
        }
        foreach (var r in ResultProperties)
        {
            hash = Hash.Combine(hash, r.GetHashCode());
        }
        return hash;
    }
}

internal sealed class ToolResultPropertyInfo : IEquatable<ToolResultPropertyInfo>
{
    public ToolResultPropertyInfo(string propertyName, string resultKey, string typeName, bool isNullable, ParameterTypeKind typeKind)
    {
        PropertyName = propertyName;
        ResultKey = resultKey;
        TypeName = typeName;
        IsNullable = isNullable;
        TypeKind = typeKind;
    }

    public string PropertyName { get; }
    public string ResultKey { get; }
    public string TypeName { get; }
    public bool IsNullable { get; }
    public ParameterTypeKind TypeKind { get; }

    public bool Equals(ToolResultPropertyInfo? other)
        => other is not null
            && PropertyName == other.PropertyName
            && ResultKey == other.ResultKey
            && TypeName == other.TypeName
            && IsNullable == other.IsNullable
            && TypeKind == other.TypeKind;

    public override bool Equals(object? obj) => Equals(obj as ToolResultPropertyInfo);

    public override int GetHashCode()
    {
        var hash = Hash.Combine(PropertyName, ResultKey);
        hash = Hash.Combine(hash, TypeName.GetHashCode());
        hash = Hash.Combine(hash, IsNullable ? 1 : 0);
        hash = Hash.Combine(hash, (int)TypeKind);
        return hash;
    }
}

internal sealed class ToolParameterInfo : IEquatable<ToolParameterInfo>
{
    public ToolParameterInfo(string propertyName, string argumentKey, string typeName, bool isNullable, ParameterTypeKind typeKind)
    {
        PropertyName = propertyName;
        ArgumentKey = argumentKey;
        TypeName = typeName;
        IsNullable = isNullable;
        TypeKind = typeKind;
    }

    public string PropertyName { get; }
    public string ArgumentKey { get; }
    public string TypeName { get; }
    public bool IsNullable { get; }
    public ParameterTypeKind TypeKind { get; }

    public bool Equals(ToolParameterInfo? other)
        => other is not null
            && PropertyName == other.PropertyName
            && ArgumentKey == other.ArgumentKey
            && TypeName == other.TypeName
            && IsNullable == other.IsNullable
            && TypeKind == other.TypeKind;

    public override bool Equals(object? obj) => Equals(obj as ToolParameterInfo);

    public override int GetHashCode()
    {
        var hash = Hash.Combine(PropertyName, ArgumentKey);
        hash = Hash.Combine(hash, TypeName.GetHashCode());
        hash = Hash.Combine(hash, IsNullable ? 1 : 0);
        hash = Hash.Combine(hash, (int)TypeKind);
        return hash;
    }
}

internal enum ParameterTypeKind
{
    String,
    Int32,
    Int64,
    Double,
    Single,
    Decimal,
    Boolean,
    Complex
}

// Result of parsing a single [ToolBlock] declaration: either a candidate to emit, a set
// of diagnostics to report, or both. It flows through the incremental pipeline, so it is
// value-equatable via its members.
internal sealed class ToolBlockParseResult : IEquatable<ToolBlockParseResult>
{
    public ToolBlockParseResult(ToolBlockCandidate? candidate, IReadOnlyList<DiagnosticInfo> diagnostics)
    {
        Candidate = candidate;
        Diagnostics = diagnostics;
    }

    public ToolBlockCandidate? Candidate { get; }

    public IReadOnlyList<DiagnosticInfo> Diagnostics { get; }

    public bool Equals(ToolBlockParseResult? other)
        => other is not null
            && Equals(Candidate, other.Candidate)
            && Diagnostics.SequenceEqual(other.Diagnostics);

    public override bool Equals(object? obj) => Equals(obj as ToolBlockParseResult);

    public override int GetHashCode()
    {
        var hash = Candidate?.GetHashCode() ?? 0;
        foreach (var d in Diagnostics)
        {
            hash = Hash.Combine(hash, d.GetHashCode());
        }
        return hash;
    }
}

// Value-equatable diagnostic payload. We cannot flow Roslyn Location/Diagnostic through the
// incremental pipeline (they are not value-equatable), so we capture just enough to rebuild
// the diagnostic in the source-output stage.
internal sealed class DiagnosticInfo : IEquatable<DiagnosticInfo>
{
    public DiagnosticInfo(string descriptorId, LocationInfo? location, string? messageArg)
    {
        DescriptorId = descriptorId;
        Location = location;
        MessageArg = messageArg;
    }

    public string DescriptorId { get; }
    public LocationInfo? Location { get; }
    public string? MessageArg { get; }

    public bool Equals(DiagnosticInfo? other)
        => other is not null
            && DescriptorId == other.DescriptorId
            && Equals(Location, other.Location)
            && MessageArg == other.MessageArg;

    public override bool Equals(object? obj) => Equals(obj as DiagnosticInfo);

    public override int GetHashCode()
        => Hash.Combine(Hash.Combine(DescriptorId, MessageArg), Location?.GetHashCode() ?? 0);
}

internal sealed class LocationInfo : IEquatable<LocationInfo>
{
    public LocationInfo(string filePath, Microsoft.CodeAnalysis.Text.TextSpan textSpan, Microsoft.CodeAnalysis.Text.LinePositionSpan lineSpan)
    {
        FilePath = filePath;
        TextSpan = textSpan;
        LineSpan = lineSpan;
    }

    public string FilePath { get; }
    public Microsoft.CodeAnalysis.Text.TextSpan TextSpan { get; }
    public Microsoft.CodeAnalysis.Text.LinePositionSpan LineSpan { get; }

    public Microsoft.CodeAnalysis.Location ToLocation()
        => Microsoft.CodeAnalysis.Location.Create(FilePath, TextSpan, LineSpan);

    public static LocationInfo? From(Microsoft.CodeAnalysis.Location location)
    {
        if (location.SourceTree is null)
        {
            return null;
        }

        return new LocationInfo(location.SourceTree.FilePath, location.SourceSpan, location.GetLineSpan().Span);
    }

    public bool Equals(LocationInfo? other)
        => other is not null
            && FilePath == other.FilePath
            && TextSpan.Equals(other.TextSpan)
            && LineSpan.Equals(other.LineSpan);

    public override bool Equals(object? obj) => Equals(obj as LocationInfo);

    public override int GetHashCode() => Hash.Combine(FilePath.GetHashCode(), TextSpan.GetHashCode());
}

internal static class Hash
{
    public static int Combine(int h1, int h2)
    {
        unchecked
        {
            var rol5 = ((uint)h1 << 5) | ((uint)h1 >> 27);
            return ((int)rol5 + h1) ^ h2;
        }
    }

    public static int Combine(string? s1, string? s2)
        => Combine(s1?.GetHashCode() ?? 0, s2?.GetHashCode() ?? 0);
}
