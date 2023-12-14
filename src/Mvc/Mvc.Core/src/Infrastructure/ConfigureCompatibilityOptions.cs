// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

/// <summary>
/// A base class for infrastructure that implements ASP.NET Core MVC's support for
/// <see cref="CompatibilityVersion"/>. This is framework infrastructure and should not be used
/// by application code.
/// </summary>
/// <typeparam name="TOptions"></typeparam>
[Obsolete("This API is obsolete and will be removed in a future version. Consider removing usages.",
    DiagnosticId = "ASP5001",
    UrlFormat = "https://aka.ms/aspnetcore-warnings/{0}")]
public abstract class ConfigureCompatibilityOptions<TOptions> : IPostConfigureOptions<TOptions>
    where TOptions : class, IEnumerable<ICompatibilitySwitch>
{
    private readonly ILogger _logger;

    /// <summary>
    /// Creates a new <see cref="ConfigureCompatibilityOptions{TOptions}"/>.
    /// </summary>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
    /// <param name="compatibilityOptions">The <see cref="IOptions{MvcCompatibilityOptions}"/>.</param>
    protected ConfigureCompatibilityOptions(
        ILoggerFactory loggerFactory,
        IOptions<MvcCompatibilityOptions> compatibilityOptions)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);

        Version = compatibilityOptions.Value.CompatibilityVersion;
        _logger = loggerFactory.CreateLogger<TOptions>();
    }

    /// <summary>
    /// Gets the default values of compatibility switches associated with the applications configured
    /// <see cref="CompatibilityVersion"/>.
    /// </summary>
    protected abstract IReadOnlyDictionary<string, object> DefaultValues { get; }

    /// <summary>
    /// Gets the <see cref="CompatibilityVersion"/> configured for the application.
    /// </summary>
    protected CompatibilityVersion Version { get; }

    /// <inheritdoc />
    public virtual void PostConfigure(string? name, TOptions options)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(options);

        // Evaluate DefaultValues once so subclasses don't have to cache.
        var defaultValues = DefaultValues;

        foreach (var @switch in options)
        {
            ConfigureSwitch(@switch, defaultValues);
        }
    }

    private void ConfigureSwitch(ICompatibilitySwitch @switch, IReadOnlyDictionary<string, object> defaultValues)
    {
        if (@switch.IsValueSet)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug(
                    "Compatibility switch {SwitchName} in type {OptionsType} is using explicitly configured value {Value}",
                    @switch.Name,
                    typeof(TOptions).Name,
                    @switch.Value);
            }
            return;
        }

        if (!defaultValues.TryGetValue(@switch.Name, out var value))
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug(
                "Compatibility switch {SwitchName} in type {OptionsType} is using default value {Value}",
                @switch.Name,
                typeof(TOptions).Name,
                @switch.Value);
            }
            return;
        }

        @switch.Value = value;
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug(
                "Compatibility switch {SwitchName} in type {OptionsType} is using compatibility value {Value} for version {Version}",
                @switch.Name,
                typeof(TOptions).Name,
                @switch.Value,
                Version);
        }
    }
}
