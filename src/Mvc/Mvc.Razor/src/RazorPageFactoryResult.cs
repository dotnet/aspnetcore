// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;

namespace Microsoft.AspNetCore.Mvc.Razor;

/// <summary>
/// Result of <see cref="IRazorPageFactoryProvider.CreateFactory(string)"/>.
/// </summary>
public readonly struct RazorPageFactoryResult
{
    /// <summary>
    /// Initializes a new instance of <see cref="RazorPageFactoryResult"/> with the
    /// specified <see cref="IRazorPage"/> factory.
    /// </summary>
    /// <param name="razorPageFactory">The <see cref="IRazorPage"/> factory.</param>
    /// <param name="viewDescriptor">The <see cref="CompiledViewDescriptor"/>.</param>
    public RazorPageFactoryResult(
        CompiledViewDescriptor viewDescriptor,
        Func<IRazorPage>? razorPageFactory)
    {
        ViewDescriptor = viewDescriptor ?? throw new ArgumentNullException(nameof(viewDescriptor));
        RazorPageFactory = razorPageFactory;
    }

    /// <summary>
    /// The <see cref="IRazorPage"/> factory.
    /// </summary>
    /// <remarks>This property is <c>null</c> when <see cref="Success"/> is <c>false</c>.</remarks>
    public Func<IRazorPage>? RazorPageFactory { get; }

    /// <summary>
    /// Gets the <see cref="CompiledViewDescriptor"/>.
    /// </summary>
    public CompiledViewDescriptor? ViewDescriptor { get; }

    /// <summary>
    /// Gets a value that determines if the page was successfully located.
    /// </summary>
    [MemberNotNullWhen(true, nameof(RazorPageFactory))]
    public bool Success => RazorPageFactory != null;
}
