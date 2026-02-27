// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Validation.Localization;

namespace Microsoft.Extensions.Validation.LocalizationTests.Helpers;

internal static class TestFormatterRegistryFactory
{
    /// <summary>
    /// Creates a <see cref="ValidationAttributeFormatterRegistry"/> with the built-in formatters
    /// registered, matching what <c>AddValidationLocalization</c> configures at runtime.
    /// </summary>
    internal static ValidationAttributeFormatterRegistry Create()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddValidation();
        services.AddValidationLocalization();
        var sp = services.BuildServiceProvider();
        return sp.GetRequiredService<IOptions<ValidationAttributeFormatterRegistry>>().Value;
    }
}
