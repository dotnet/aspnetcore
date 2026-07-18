// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Validation.Localization;

internal sealed class ValidationLocalizationSetup(
    IOptions<ValidationLocalizationOptions> localizationOptions,
    IStringLocalizerFactory stringLocalizerFactory)
    : IConfigureOptions<ValidationOptions>
{
    public void Configure(ValidationOptions options)
    {
        options.Localizer ??= new DefaultValidationLocalizer(stringLocalizerFactory, localizationOptions);
    }
}
