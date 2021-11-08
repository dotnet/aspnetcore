// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions;

internal class RazorExtensionsDiagnosticFactory
{
    private const string DiagnosticPrefix = "RZ";

    internal static readonly RazorDiagnosticDescriptor ViewComponent_CannotFindMethod =
        new RazorDiagnosticDescriptor(
            $"{DiagnosticPrefix}3900",
            () => RazorExtensionsResources.ViewComponent_CannotFindMethod,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateViewComponent_CannotFindMethod(string tagHelperType)
    {
        var diagnostic = RazorDiagnostic.Create(
            ViewComponent_CannotFindMethod,
            new SourceSpan(SourceLocation.Undefined, contentLength: 0),
            ViewComponentTypes.SyncMethodName,
            ViewComponentTypes.AsyncMethodName,
            tagHelperType);

        return diagnostic;
    }

    internal static readonly RazorDiagnosticDescriptor ViewComponent_AmbiguousMethods =
        new RazorDiagnosticDescriptor(
            $"{DiagnosticPrefix}3901",
            () => RazorExtensionsResources.ViewComponent_AmbiguousMethods,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateViewComponent_AmbiguousMethods(string tagHelperType)
    {
        var diagnostic = RazorDiagnostic.Create(
            ViewComponent_AmbiguousMethods,
            new SourceSpan(SourceLocation.Undefined, contentLength: 0),
            tagHelperType,
            ViewComponentTypes.SyncMethodName,
            ViewComponentTypes.AsyncMethodName);

        return diagnostic;
    }

    internal static readonly RazorDiagnosticDescriptor ViewComponent_AsyncMethod_ShouldReturnTask =
        new RazorDiagnosticDescriptor(
            $"{DiagnosticPrefix}3902",
            () => RazorExtensionsResources.ViewComponent_AsyncMethod_ShouldReturnTask,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateViewComponent_AsyncMethod_ShouldReturnTask(string tagHelperType)
    {
        var diagnostic = RazorDiagnostic.Create(
            ViewComponent_AsyncMethod_ShouldReturnTask,
            new SourceSpan(SourceLocation.Undefined, contentLength: 0),
            ViewComponentTypes.AsyncMethodName,
            tagHelperType,
            nameof(Task));

        return diagnostic;
    }

    internal static readonly RazorDiagnosticDescriptor ViewComponent_SyncMethod_ShouldReturnValue =
        new RazorDiagnosticDescriptor(
            $"{DiagnosticPrefix}3903",
            () => RazorExtensionsResources.ViewComponent_SyncMethod_ShouldReturnValue,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateViewComponent_SyncMethod_ShouldReturnValue(string tagHelperType)
    {
        var diagnostic = RazorDiagnostic.Create(
            ViewComponent_SyncMethod_ShouldReturnValue,
            new SourceSpan(SourceLocation.Undefined, contentLength: 0),
            ViewComponentTypes.SyncMethodName,
            tagHelperType);

        return diagnostic;
    }

    internal static readonly RazorDiagnosticDescriptor ViewComponent_SyncMethod_CannotReturnTask =
        new RazorDiagnosticDescriptor(
            $"{DiagnosticPrefix}3904",
            () => RazorExtensionsResources.ViewComponent_SyncMethod_CannotReturnTask,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreateViewComponent_SyncMethod_CannotReturnTask(string tagHelperType)
    {
        var diagnostic = RazorDiagnostic.Create(
            ViewComponent_SyncMethod_CannotReturnTask,
            new SourceSpan(SourceLocation.Undefined, contentLength: 0),
            ViewComponentTypes.SyncMethodName,
            tagHelperType,
            nameof(Task));

        return diagnostic;
    }

    internal static readonly RazorDiagnosticDescriptor PageDirective_CannotBeImported =
        new RazorDiagnosticDescriptor(
            $"{DiagnosticPrefix}3905",
            () => RazorExtensionsResources.PageDirectiveCannotBeImported,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreatePageDirective_CannotBeImported(SourceSpan source)
    {
        var fileName = Path.GetFileName(source.FilePath);
        var diagnostic = RazorDiagnostic.Create(PageDirective_CannotBeImported, source, PageDirective.Directive.Directive, fileName);

        return diagnostic;
    }

    internal static readonly RazorDiagnosticDescriptor PageDirective_MustExistAtTheTopOfFile =
        new RazorDiagnosticDescriptor(
            $"{DiagnosticPrefix}3906",
            () => RazorExtensionsResources.PageDirectiveMustExistAtTheTopOfFile,
            RazorDiagnosticSeverity.Error);

    public static RazorDiagnostic CreatePageDirective_MustExistAtTheTopOfFile(SourceSpan source)
    {
        var diagnostic = RazorDiagnostic.Create(PageDirective_MustExistAtTheTopOfFile, source, PageDirective.Directive.Directive);
        return diagnostic;
    }
}
