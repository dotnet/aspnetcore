// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.JSInterop.SourceGenerators;

internal enum ReturnKind
{
    Void,
    Task,
    TaskOfT,
    Value,
}

internal sealed record JSInvokableMethod(
    string Identifier,
    bool IsStatic,
    string ContainingType,
    string MethodName,
    EquatableArray<string> ParameterTypes,
    ReturnKind ReturnKind,
    string? ResultType,
    LocationInfo? Location,
    HostInfo? Host = null,
    bool IsHidden = false);

// A partial containing type that registers its invokers from a generated static constructor. Generated
// code inside the type can name its type parameters and reach non-accessible nested types.
internal sealed record HostInfo(string? Namespace, EquatableArray<string> Declarations, string ConstructorName, string HintName);

internal sealed record DiagnosticInfo(string DescriptorId, LocationInfo? Location, EquatableArray<string> MessageArgs)
{
    public Diagnostic ToDiagnostic()
        => Diagnostic.Create(DiagnosticDescriptors.ById(DescriptorId), Location?.ToLocation() ?? Microsoft.CodeAnalysis.Location.None, [.. MessageArgs]);
}

internal sealed record ParseResult(EquatableArray<JSInvokableMethod> Methods, EquatableArray<DiagnosticInfo> Diagnostics);

internal sealed record LocationInfo(string FilePath, TextSpan Span, LinePositionSpan LineSpan)
{
    public static LocationInfo? From(Location? location)
        => location?.SourceTree is null ? null : new(location.SourceTree.FilePath, location.SourceSpan, location.GetLineSpan().Span);

    public Location ToLocation() => Location.Create(FilePath, Span, LineSpan);
}
