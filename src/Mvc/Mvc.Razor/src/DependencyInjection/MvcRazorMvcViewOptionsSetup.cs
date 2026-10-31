// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Configures <see cref="MvcViewOptions"/> to use <see cref="RazorViewEngine"/>.
/// </summary>
internal sealed class MvcRazorMvcViewOptionsSetup : IConfigureOptions<MvcViewOptions>
{
    private readonly IRazorViewEngine _razorViewEngine;

    /// <summary>
    /// Initializes a new instance of <see cref="MvcRazorMvcViewOptionsSetup"/>.
    /// </summary>
    /// <param name="razorViewEngine">The <see cref="IRazorViewEngine"/>.</param>
    public MvcRazorMvcViewOptionsSetup(IRazorViewEngine razorViewEngine)
    {
        ArgumentNullException.ThrowIfNull(razorViewEngine);

        _razorViewEngine = razorViewEngine;
    }

    /// <summary>
    /// Configures <paramref name="options"/> to use <see cref="RazorViewEngine"/>.
    /// </summary>
    /// <param name="options">The <see cref="MvcViewOptions"/> to configure.</param>
    public void Configure(MvcViewOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.ViewEngines.Add(_razorViewEngine);
    }
}
