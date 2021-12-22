// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.Configuration.KeyPerFile;

/// <summary>
/// A <see cref="ConfigurationProvider"/> that uses a directory's files as configuration key/values.
/// </summary>
public class KeyPerFileConfigurationProvider : ConfigurationProvider, IDisposable
{
    private readonly IDisposable? _changeTokenRegistration;

    KeyPerFileConfigurationSource Source { get; set; }

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    /// <param name="source">The settings.</param>
    public KeyPerFileConfigurationProvider(KeyPerFileConfigurationSource source)
    {
        Source = source ?? throw new ArgumentNullException(nameof(source));

        if (Source.ReloadOnChange && Source.FileProvider != null)
        {
            _changeTokenRegistration = ChangeToken.OnChange(
                () => Source.FileProvider.Watch("*"),
                () =>
                {
                    Thread.Sleep(Source.ReloadDelay);
                    Load(reload: true);
                });
        }

    }

    private string NormalizeKey(string key)
        => key.Replace(Source.SectionDelimiter, ConfigurationPath.KeyDelimiter);

    private static string TrimNewLine(string value)
        => value.EndsWith(Environment.NewLine, StringComparison.Ordinal)
            ? value.Substring(0, value.Length - Environment.NewLine.Length)
            : value;

    /// <summary>
    /// Loads the configuration values.
    /// </summary>
    public override void Load()
    {
        Load(reload: false);
    }

    private void Load(bool reload)
    {
        var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        if (Source.FileProvider == null)
        {
            if (Source.Optional || reload) // Always optional on reload
            {
                Data = data;
                OnReload();
                return;
            }

            throw new DirectoryNotFoundException("A non-null file provider for the directory is required when this source is not optional.");
        }

        var directory = Source.FileProvider.GetDirectoryContents("/");
        if (!directory.Exists)
        {
            if (Source.Optional || reload) // Always optional on reload
            {
                Data = data;
                OnReload();
                return;
            }
            throw new DirectoryNotFoundException("The root directory for the FileProvider doesn't exist and is not optional.");
        }
        else
        {
            foreach (var file in directory)
            {
                if (file.IsDirectory)
                {
                    continue;
                }

                using var stream = file.CreateReadStream();
                using var streamReader = new StreamReader(stream);

                if (Source.IgnoreCondition == null || !Source.IgnoreCondition(file.Name))
                {
                    data.Add(NormalizeKey(file.Name), TrimNewLine(streamReader.ReadToEnd()));
                }

            }
        }

        Data = data;
        OnReload();
    }

    private string GetDirectoryName()
        => Source.FileProvider?.GetFileInfo("/")?.PhysicalPath ?? "<Unknown>";

    /// <summary>
    /// Generates a string representing this provider name and relevant details.
    /// </summary>
    /// <returns>The configuration name.</returns>
    public override string ToString()
        => $"{GetType().Name} for files in '{GetDirectoryName()}' ({(Source.Optional ? "Optional" : "Required")})";

    /// <inheritdoc />
    public void Dispose()
    {
        _changeTokenRegistration?.Dispose();
    }
}
