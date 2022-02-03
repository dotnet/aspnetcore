// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.Extensions.Configuration.KeyPerFile;

/// <summary>
/// An <see cref="IConfigurationSource"/> used to configure <see cref="KeyPerFileConfigurationProvider"/>.
/// </summary>
public class KeyPerFileConfigurationSource : IConfigurationSource
{
    /// <summary>
    /// Constructor;
    /// </summary>
    public KeyPerFileConfigurationSource()
        => IgnoreCondition = s => IgnorePrefix != null && s.StartsWith(IgnorePrefix, StringComparison.Ordinal);

    /// <summary>
    /// The FileProvider whos root "/" directory files will be used as configuration data.
    /// </summary>
    public IFileProvider? FileProvider { get; set; }

    /// <summary>
    /// Files that start with this prefix will be excluded.
    /// Defaults to "ignore.".
    /// </summary>
    public string IgnorePrefix { get; set; } = "ignore.";

    /// <summary>
    /// Used to determine if a file should be ignored using its name.
    /// Defaults to using the IgnorePrefix.
    /// </summary>
    public Func<string, bool> IgnoreCondition { get; set; }

    /// <summary>
    /// If false, will throw if the directory doesn't exist.
    /// </summary>
    public bool Optional { get; set; }

    /// <summary>
    /// Determines whether the source will be loaded if the underlying file changes.
    /// </summary>
    public bool ReloadOnChange { get; set; }

    /// <summary>
    /// Number of milliseconds that reload will wait before calling Load.  This helps
    /// avoid triggering reload before a file is completely written. Default is 250.
    /// </summary>
    public int ReloadDelay { get; set; } = 250;

    /// <summary>
    /// The delimiter used to separate individual keys in a path.
    /// </summary>
    /// <value>Default is <c>__</c>.</value>
    public string SectionDelimiter { get; set; } = "__";

    /// <summary>
    /// Builds the <see cref="KeyPerFileConfigurationProvider"/> for this source.
    /// </summary>
    /// <param name="builder">The <see cref="IConfigurationBuilder"/>.</param>
    /// <returns>A <see cref="KeyPerFileConfigurationProvider"/></returns>
    public IConfigurationProvider Build(IConfigurationBuilder builder)
        => new KeyPerFileConfigurationProvider(this);
}
