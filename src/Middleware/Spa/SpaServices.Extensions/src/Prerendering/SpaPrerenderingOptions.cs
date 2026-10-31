// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SpaServices.Prerendering;

#nullable disable

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Represents options for the SPA prerendering middleware.
/// </summary>
[Obsolete("Prerendering is no longer supported out of box")]
public class SpaPrerenderingOptions
{
    /// <summary>
    /// Gets or sets an <see cref="ISpaPrerendererBuilder"/> that the prerenderer will invoke before
    /// looking for the boot module file.
    ///
    /// This is only intended to be used during development as a way of generating the JavaScript boot
    /// file automatically when the application runs. This property should be left as <c>null</c> in
    /// production applications.
    /// </summary>
    public ISpaPrerendererBuilder BootModuleBuilder { get; set; }

    /// <summary>
    /// Gets or sets the path, relative to your application root, of the JavaScript file
    /// containing prerendering logic.
    /// </summary>
    public string BootModulePath { get; set; }

    /// <summary>
    /// Gets or sets an array of URL prefixes for which prerendering should not run.
    /// </summary>
    public string[] ExcludeUrls { get; set; }

    /// <summary>
    /// Gets or sets a callback that will be invoked during prerendering, allowing you to pass additional
    /// data to the prerendering entrypoint code.
    /// </summary>
    public Action<HttpContext, IDictionary<string, object>> SupplyData { get; set; }
}
