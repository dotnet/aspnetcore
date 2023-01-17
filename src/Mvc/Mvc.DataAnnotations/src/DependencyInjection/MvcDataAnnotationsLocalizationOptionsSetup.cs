// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.DataAnnotations;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Sets up default options for <see cref="MvcDataAnnotationsLocalizationOptions"/>.
/// </summary>
internal sealed class MvcDataAnnotationsLocalizationOptionsSetup : IConfigureOptions<MvcDataAnnotationsLocalizationOptions>
{
    /// <inheritdoc />
    public void Configure(MvcDataAnnotationsLocalizationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.DataAnnotationLocalizerProvider = (modelType, stringLocalizerFactory) =>
            stringLocalizerFactory.Create(modelType);
    }
}
