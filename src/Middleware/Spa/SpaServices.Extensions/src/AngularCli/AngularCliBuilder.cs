// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.NodeServices.Npm;
using Microsoft.AspNetCore.NodeServices.Util;
using Microsoft.AspNetCore.SpaServices.Prerendering;
using Microsoft.AspNetCore.SpaServices.Util;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.SpaServices.AngularCli;

/// <summary>
/// Provides an implementation of <see cref="ISpaPrerendererBuilder"/> that can build
/// an Angular application by invoking the Angular CLI.
/// </summary>
[Obsolete("Prerendering is no longer supported out of box")]
public class AngularCliBuilder : ISpaPrerendererBuilder
{
    private static readonly TimeSpan RegexMatchTimeout = TimeSpan.FromSeconds(5); // This is a development-time only feature, so a very long timeout is fine

    private readonly string _scriptName;

    /// <summary>
    /// Constructs an instance of <see cref="AngularCliBuilder"/>.
    /// </summary>
    /// <param name="npmScript">The name of the script in your package.json file that builds the server-side bundle for your Angular application.</param>
    public AngularCliBuilder(string npmScript)
    {
        ArgumentException.ThrowIfNullOrEmpty(npmScript);

        _scriptName = npmScript;
    }

    /// <inheritdoc />
    public async Task Build(ISpaBuilder spaBuilder)
    {
        var pkgManagerCommand = spaBuilder.Options.PackageManagerCommand;
        var sourcePath = spaBuilder.Options.SourcePath;
        if (string.IsNullOrEmpty(sourcePath))
        {
            throw new InvalidOperationException($"To use {nameof(AngularCliBuilder)}, you must supply a non-empty value for the {nameof(SpaOptions.SourcePath)} property of {nameof(SpaOptions)} when calling {nameof(SpaApplicationBuilderExtensions.UseSpa)}.");
        }

        var appBuilder = spaBuilder.ApplicationBuilder;
        var applicationStoppingToken = appBuilder.ApplicationServices.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping;
        var logger = LoggerFinder.GetOrCreateLogger(
            appBuilder,
            nameof(AngularCliBuilder));
        var diagnosticSource = appBuilder.ApplicationServices.GetRequiredService<DiagnosticSource>();
        var scriptRunner = new NodeScriptRunner(
            sourcePath,
            _scriptName,
            "--watch",
            null,
            pkgManagerCommand,
            diagnosticSource,
            applicationStoppingToken);
        scriptRunner.AttachToLogger(logger);

        using (var stdOutReader = new EventedStreamStringReader(scriptRunner.StdOut))
        using (var stdErrReader = new EventedStreamStringReader(scriptRunner.StdErr))
        {
            try
            {
                await scriptRunner.StdOut.WaitForMatch(
                    new Regex("Date", RegexOptions.None, RegexMatchTimeout));
            }
            catch (EndOfStreamException ex)
            {
                throw new InvalidOperationException(
                    $"The {pkgManagerCommand} script '{_scriptName}' exited without indicating success.\n" +
                    $"Output was: {stdOutReader.ReadAsString()}\n" +
                    $"Error output was: {stdErrReader.ReadAsString()}", ex);
            }
            catch (OperationCanceledException ex)
            {
                throw new InvalidOperationException(
                    $"The {pkgManagerCommand} script '{_scriptName}' timed out without indicating success. " +
                    $"Output was: {stdOutReader.ReadAsString()}\n" +
                    $"Error output was: {stdErrReader.ReadAsString()}", ex);
            }
        }
    }
}
