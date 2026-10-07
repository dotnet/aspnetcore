// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This file contains stub implementations of all public API types for source-build on non-Windows platforms.
// These stubs allow reference assemblies to contain the correct API surface so that code referencing
// IIS types can compile on any platform, even though IIS only works on Windows at runtime.

using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

[assembly: TypeForwardedTo(typeof(IServerVariablesFeature))]

namespace Microsoft.AspNetCore.Hosting
{
    /// <summary>
    /// Extension methods for the IIS In-Process.
    /// </summary>
    public static class WebHostBuilderIISExtensions
    {
        /// <summary>
        /// Configures the port and base path the server should listen on when running behind AspNetCoreModule.
        /// The app will also be configured to capture startup errors.
        /// </summary>
        /// <param name="hostBuilder">The <see cref="IWebHostBuilder"/> to configure.</param>
        /// <returns>The <see cref="IWebHostBuilder"/>.</returns>
        public static IWebHostBuilder UseIIS(this IWebHostBuilder hostBuilder) => hostBuilder;
    }
}

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Provides configuration for IIS In-Process.
    /// </summary>
    public class IISServerOptions
    {
        /// <summary>
        /// Gets or sets a value that controls whether synchronous IO is allowed for the <see cref="HttpContext.Request"/> and <see cref="HttpContext.Response"/>
        /// </summary>
        public bool AllowSynchronousIO { get; set; }

        /// <summary>
        /// If true the server should set HttpContext.User. If false the server will only provide an
        /// identity when explicitly requested by the AuthenticationScheme.
        /// </summary>
        public bool AutomaticAuthentication { get; set; } = true;

        /// <summary>
        /// Sets the display name shown to users on login pages. The default is null.
        /// </summary>
        public string? AuthenticationDisplayName { get; set; }

        /// <summary>
        /// Gets or sets the maximum unconsumed incoming bytes the server will buffer for incoming request body.
        /// </summary>
        public int MaxRequestBodyBufferSize { get; set; } = 1024 * 1024;

        /// <summary>
        /// Gets or sets the maximum allowed size of any request body in bytes.
        /// When set to null, the maximum request length will not be restricted in ASP.NET Core.
        /// </summary>
        public long? MaxRequestBodySize { get; set; } = 30000000;
    }
}

namespace Microsoft.AspNetCore.Server.IIS
{
    /// <summary>
    /// String constants used to configure IIS In-Process.
    /// </summary>
    public class IISServerDefaults
    {
        /// <summary>
        /// Default authentication scheme, which is "Windows".
        /// </summary>
        public const string AuthenticationScheme = "Windows";
    }

    ///<inheritdoc/>
    [Obsolete("Moved to Microsoft.AspNetCore.Http.BadHttpRequestException. See https://aka.ms/badhttprequestexception for details.")]
    public sealed class BadHttpRequestException : Microsoft.AspNetCore.Http.BadHttpRequestException
    {
        internal BadHttpRequestException(string message, int statusCode)
            : base(message, statusCode)
        {
        }

        ///<inheritdoc/>
        public new int StatusCode { get => base.StatusCode; }
    }

    /// <summary>
    /// Extensions to <see cref="HttpContext"/> that enable access to IIS features.
    /// </summary>
    public static class HttpContextExtensions
    {
        /// <summary>
        /// Gets the value of a server variable for the current request.
        /// </summary>
        /// <param name="context">The http context for the request.</param>
        /// <param name="variableName">The name of the variable.</param>
        /// <returns>
        /// <c>null</c> if the server does not support the <see cref="IServerVariablesFeature"/> feature.
        /// May return null or empty if the variable does not exist or is not set.
        /// </returns>
        [Obsolete("This is obsolete and will be removed in a future version. Use HttpContextServerVariableExtensions.GetServerVariable instead.")]
        public static string? GetIISServerVariable(this HttpContext context, string variableName) => null;
    }

    /// <summary>
    /// This feature provides access to IIS application information
    /// </summary>
    public interface IIISEnvironmentFeature
    {
        /// <summary>
        /// Gets the version of IIS that is being used.
        /// </summary>
        Version IISVersion { get; }

        /// <summary>
        /// Gets the AppPool name that is currently running
        /// </summary>
        string AppPoolId { get; }

        /// <summary>
        /// Gets the path to the AppPool config
        /// </summary>
        string AppPoolConfigFile { get; }

        /// <summary>
        /// Gets path to the application configuration that is currently running
        /// </summary>
        string AppConfigPath { get; }

        /// <summary>
        /// Gets the physical path of the application.
        /// </summary>
        string ApplicationPhysicalPath { get; }

        /// <summary>
        /// Gets the virtual path of the application.
        /// </summary>
        string ApplicationVirtualPath { get; }

        /// <summary>
        /// Gets ID of the current application.
        /// </summary>
        string ApplicationId { get; }

        /// <summary>
        /// Gets the name of the current site.
        /// </summary>
        string SiteName { get; }

        /// <summary>
        /// Gets the id of the current site.
        /// </summary>
        uint SiteId { get; }
    }
}
