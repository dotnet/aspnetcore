// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.SpaServices.StaticFiles;

/// <summary>
/// Provides an implementation of <see cref="ISpaStaticFileProvider"/> that supplies
/// physical files at a location configured using <see cref="SpaStaticFilesOptions"/>.
/// </summary>
internal sealed class DefaultSpaStaticFileProvider : ISpaStaticFileProvider
{
    private readonly IFileProvider? _fileProvider;

    public DefaultSpaStaticFileProvider(
        IServiceProvider serviceProvider,
        SpaStaticFilesOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrEmpty(options.RootPath))
        {
            throw new ArgumentException($"The {nameof(options.RootPath)} property " +
                $"of {nameof(options)} cannot be null or empty.");
        }

        var env = serviceProvider.GetRequiredService<IWebHostEnvironment>();
        var absoluteRootPath = Path.Combine(
            env.ContentRootPath,
            options.RootPath);

        // PhysicalFileProvider will throw if you pass a non-existent path,
        // but we don't want that scenario to be an error because for SPA
        // scenarios, it's better if non-existing directory just means we
        // don't serve any static files.
        if (Directory.Exists(absoluteRootPath))
        {
            _fileProvider = new PhysicalFileProvider(absoluteRootPath);
        }
    }

    public IFileProvider? FileProvider => _fileProvider;
}
