// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// Specifies the version compatibility of runtime behaviors configured by <see cref="MvcOptions"/>.
/// </summary>
/// <remarks>
/// <para>
/// The best way to set a compatibility version is by using
/// <see cref="MvcCoreMvcBuilderExtensions.SetCompatibilityVersion"/> or
/// <see cref="MvcCoreMvcCoreBuilderExtensions.SetCompatibilityVersion"/> in your application's
/// <c>ConfigureServices</c> method.
/// <example>
/// Setting the compatibility version using <see cref="IMvcBuilder"/>:
/// <code>
/// public class Startup
/// {
///     ...
///
///     public void ConfigureServices(IServiceCollection services)
///     {
///         services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_3_0);
///     }
///
///     ...
/// }
/// </code>
/// </example>
/// </para>
/// <para>
/// Setting compatibility version to a specific version will change the default values of various
/// settings to match a particular minor release of ASP.NET Core MVC.
/// </para>
/// </remarks>
// Remove after .NET 6.
[Obsolete("This API is obsolete and will be removed in a future version. Consider removing usages.",
    DiagnosticId = "ASP5001",
    UrlFormat = "https://aka.ms/aspnetcore-warnings/{0}")]
public enum CompatibilityVersion
{
    /// <summary>
    /// Sets the default value of settings on <see cref="MvcOptions"/> to match the behavior of
    /// ASP.NET Core MVC 2.0.
    /// </summary>
    [Obsolete("This " + nameof(CompatibilityVersion) + " value is obsolete.")]
    Version_2_0,

    /// <summary>
    /// Sets the default value of settings on <see cref="MvcOptions"/> to match the behavior of
    /// ASP.NET Core MVC 2.1.
    /// </summary>
    /// <remarks>
    /// ASP.NET Core MVC 2.1 introduced a compatibility switch for
    /// <c>MvcJsonOptions.AllowInputFormatterExceptionMessages</c>. This is now a regular property.
    /// </remarks>
    [Obsolete("This " + nameof(CompatibilityVersion) + " value is obsolete.")]
    Version_2_1,

    /// <summary>
    /// Sets the default value of settings on <see cref="MvcOptions"/> to match the behavior of
    /// ASP.NET Core MVC 2.2.
    /// </summary>
    /// <remarks>
    /// ASP.NET Core MVC 2.2 introduced compatibility switches for the following:
    /// <list type="bullet">
    ///     <item><description><c>ApiBehaviorOptions.SuppressMapClientErrors</c></description></item>
    ///     <item><description><see cref="MvcOptions.EnableEndpointRouting" /></description></item>
    ///     <item><description><see cref="MvcOptions.MaxValidationDepth" /></description></item>
    /// </list>
    /// All of the above are now regular properties.
    /// </remarks>
    [Obsolete("This " + nameof(CompatibilityVersion) + " value is obsolete.")]
    Version_2_2,

    /// <summary>
    /// Sets the default value of settings on <see cref="MvcOptions"/> and other <c>Options</c> types to match
    /// the behavior of ASP.NET Core MVC 3.0.
    /// </summary>
    [Obsolete("This " + nameof(CompatibilityVersion) + " value is obsolete.")]
    Version_3_0,

    /// <summary>
    /// Sets the default value of settings on <see cref="MvcOptions"/> to match the latest release. Use this
    /// value with care, upgrading minor versions will cause breaking changes when using <see cref="Latest"/>.
    /// </summary>
    [Obsolete("This " + nameof(CompatibilityVersion) + " value is obsolete.")]
    Latest = int.MaxValue,
}
