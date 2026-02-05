// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Localization;

namespace MinimalValidationSample;

public class MockStringLocalizer : IStringLocalizer
{
    public static MockStringLocalizer Instance { get; } = new();

    public LocalizedString this[string name] => new(name, $"LOCALIZED: {name}");

    public LocalizedString this[string name, params object[] arguments] => new(name, $"LOCALIZED: {name} (with args)");

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
    {
        return [new("test", $"LOCALIZED: test (all)")];
    }
}
