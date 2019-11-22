// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.Infrastructure
{
    /// <summary>
    /// A base class for infrastructure that implements ASP.NET Core MVC's support for
    /// <see cref="CompatibilityVersion"/>. This is framework infrastructure and should not be used
    /// by application code.
    /// </summary>
    /// <typeparam name="TOptions"></typeparam>
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
            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

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
        public virtual void PostConfigure(string name, TOptions options)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

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
                _logger.LogDebug(
                    "Compatibility switch {SwitchName} in type {OptionsType} is using explicitly configured value {Value}",
                    @switch.Name,
                    typeof(TOptions).Name,
                    @switch.Value);
                return;
            }

            if (!defaultValues.TryGetValue(@switch.Name, out var value))
            {
                _logger.LogDebug(
                    "Compatibility switch {SwitchName} in type {OptionsType} is using default value {Value}",
                    @switch.Name,
                    typeof(TOptions).Name,
                    @switch.Value,
                    Version);
                return;
            }

            @switch.Value = value;
            _logger.LogDebug(
                "Compatibility switch {SwitchName} in type {OptionsType} is using compatibility value {Value} for version {Version}",
                @switch.Name,
                typeof(TOptions).Name,
                @switch.Value,
                Version);
        }
    }
}
