// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting.Views;
using Microsoft.AspNetCore.Server.IIS;
using Microsoft.Extensions.FileProviders;

/// <summary>
/// Startup hooks are pieces of code that will run before a users program main executes
/// See: <see href="https://github.com/dotnet/core-setup/blob/master/Documentation/design-docs/host-startup-hook.md"/>
/// The type must be named StartupHook without any namespace, and should be internal.
/// </summary>
internal sealed class StartupHook
{
    /// <summary>
    /// Startup hooks are pieces of code that will run before a users program main executes
    /// See: <see href="https://github.com/dotnet/core-setup/blob/master/Documentation/design-docs/host-startup-hook.md"/>
    /// </summary>
    public static void Initialize()
    {
        if (!NativeMethods.IsAspNetCoreModuleLoaded())
        {
            // This means someone set the startup hook for Microsoft.AspNetCore.Server.IIS
            // but are not running inprocess. Return at this point.
            return;
        }

        var detailedErrors = Environment.GetEnvironmentVariable("ASPNETCORE_DETAILEDERRORS");
        var enableStartupErrorPage = detailedErrors?.Equals("1", StringComparison.OrdinalIgnoreCase) ?? false;
        enableStartupErrorPage |= detailedErrors?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;

        var aspnetCoreEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        enableStartupErrorPage |= aspnetCoreEnvironment?.Equals("Development", StringComparison.OrdinalIgnoreCase) ?? false;

        var dotnetEnvironment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
        enableStartupErrorPage |= dotnetEnvironment?.Equals("Development", StringComparison.OrdinalIgnoreCase) ?? false;

        if (!enableStartupErrorPage)
        {
            // Not running in development or detailed errors aren't enabled
            return;
        }

        AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) =>
        {
            var exception = (Exception)eventArgs.ExceptionObject;

            // Get the content root from IIS.
            var iisConfigData = NativeMethods.HttpGetApplicationProperties();
            var contentRoot = iisConfigData.pwzFullApplicationPath.TrimEnd(Path.DirectorySeparatorChar);

            var model = ErrorPageModelBuilder.CreateErrorPageModel(
                new PhysicalFileProvider(contentRoot),
                logger: null,
                showDetailedErrors: true,
                exception);

            var errorPage = new ErrorPage(model);

            var stream = new MemoryStream();

            // Never will go async because we are writing to a memory stream.
            errorPage.ExecuteAsync(stream).GetAwaiter().GetResult();

            // Get the raw content and set the error page.
            stream.Position = 0;
            var content = stream.ToArray();

            NativeMethods.HttpSetStartupErrorPageContent(content);
        };
    }
}
