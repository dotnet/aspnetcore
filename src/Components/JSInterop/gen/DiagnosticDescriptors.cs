// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.CodeAnalysis;

namespace Microsoft.JSInterop.SourceGenerators;

internal static class DiagnosticDescriptors
{
    private const string Category = "JSInterop";

    public static readonly DiagnosticDescriptor NotPublic = Create(
        "JSI0001",
        "JS-invokable method is not public",
        "JS-invokable method '{0}' is not public; it cannot be invoked from JavaScript and is not registered for generated dispatch");

    public static readonly DiagnosticDescriptor Generic = Create(
        "JSI0002",
        "JS-invokable method cannot be invoked from JavaScript",
        "JS-invokable method '{0}' cannot be invoked from JavaScript by reflection or generated dispatch: {1}");

    public static readonly DiagnosticDescriptor InaccessibleType = Create(
        "JSI0003",
        "JS-invokable method's containing type is inaccessible to generated code",
        "JS-invokable method '{0}' is declared in a file-local type and is not registered for generated dispatch");

    public static readonly DiagnosticDescriptor StaticOnNonPublicType = Create(
        "JSI0004",
        "Static JS-invokable method is declared in a non-public type",
        "Static JS-invokable method '{0}' is declared in a type that is not publicly visible; it cannot be invoked from JavaScript and is not registered for generated dispatch");

    public static readonly DiagnosticDescriptor UnsupportedSignature = Create(
        "JSI0005",
        "JS-invokable method signature is not supported by generated dispatch",
        "JS-invokable method '{0}' is not registered for generated dispatch: {1}");

    public static readonly DiagnosticDescriptor DuplicateIdentifier = Create(
        "JSI0006",
        "Duplicate JS-invokable identifier",
        "The JS-invokable identifier '{0}' is declared more than once in the same scope; none of the duplicates is registered for generated dispatch");

    public static readonly DiagnosticDescriptor PartialTypeRequired = Create(
        "JSI0007",
        "Containing type must be partial for generated dispatch",
        "Generated dispatch cannot register '{0}' because '{1}' is generic or not accessible to the assembly; declare '{1}' and all of its containing types partial");

    public static readonly DiagnosticDescriptor StaticConstructorConflict = Create(
        "JSI0008",
        "Containing type declares a static constructor",
        "Generated dispatch cannot register '{0}' because '{1}' declares a static constructor; generic and non-accessible types are registered from a generated static constructor");

    public static DiagnosticDescriptor ById(string id) => id switch
    {
        "JSI0001" => NotPublic,
        "JSI0002" => Generic,
        "JSI0003" => InaccessibleType,
        "JSI0004" => StaticOnNonPublicType,
        "JSI0005" => UnsupportedSignature,
        "JSI0006" => DuplicateIdentifier,
        "JSI0007" => PartialTypeRequired,
        "JSI0008" => StaticConstructorConflict,
        _ => throw new ArgumentOutOfRangeException(nameof(id), id, null),
    };

    private static DiagnosticDescriptor Create(string id, string title, string message)
        => new(id, title, message, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);
}
