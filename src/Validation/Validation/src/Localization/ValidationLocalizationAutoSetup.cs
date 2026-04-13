// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Validation;

/// <summary>
/// Auto-configures <see cref="ValidationOptions"/> with IStringLocalizer-based localization
/// when <see cref="IStringLocalizerFactory"/> is available in DI.
/// </summary>
internal sealed class ValidationLocalizationAutoSetup(
    IServiceProvider serviceProvider) : IConfigureOptions<ValidationOptions>
{
    public void Configure(ValidationOptions options)
    {
        var factory = serviceProvider.GetService(typeof(IStringLocalizerFactory)) as IStringLocalizerFactory;
        if (factory is not null)
        {
            // Store the factory only. The LocalizationContext is created lazily on first
            // access so that IPostConfigureOptions callbacks (e.g. from libraries that wrap
            // LocalizerProvider or ErrorMessageKeyProvider) have already run.
            options.StringLocalizerFactory = factory;
        }
    }
}
