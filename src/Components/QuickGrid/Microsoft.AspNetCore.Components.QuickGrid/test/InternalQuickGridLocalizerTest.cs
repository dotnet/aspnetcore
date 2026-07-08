// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
using System.Resources;
using Microsoft.AspNetCore.Components.QuickGrid.Localization;
using Microsoft.Extensions.Localization;

namespace Microsoft.AspNetCore.Components.QuickGrid.Tests;

public class InternalQuickGridLocalizerTest
{
    [Fact]
    public void ReturnsDefaultPaginationPageStatusWhenNoCustomLocalizerIsRegistered()
    {
        var localizer = CreateLocalizer();

        var result = localizer["PaginationPageStatus", 1, 10];

        Assert.Equal("Page 1 of 10", result);
    }

    [Fact]
    public void ReturnsCustomPaginationPageStatusWhenCustomLocalizerProvidesValue()
    {
        var customLocalizer = new TestQuickGridLocalizer(
            "PaginationPageStatus",
            "Showing page {0} from {1}");

        var localizer = CreateLocalizer(customLocalizer);

        var result = localizer["PaginationPageStatus", 2, 8];

        Assert.Equal("Showing page 2 from 8", result);
    }

    [Fact]
    public void SupportsReorderedPaginationPageStatusPlaceholders()
    {
        var customLocalizer = new TestQuickGridLocalizer(
            "PaginationPageStatus",
            "{1} total pages, currently {0}");

        var localizer = CreateLocalizer(customLocalizer);

        var result = localizer["PaginationPageStatus", 2, 10];

        Assert.Equal("10 total pages, currently 2", result);
    }

    [Fact]
    public void FallsBackToKeyWhenCustomValueIsWhitespace()
    {
        // The default resource resolves to a single-space value; the localizer
        // must treat whitespace as missing and hand back the key verbatim.
        var customLocalizer = new TestQuickGridLocalizer(
            "WhitespaceKey",
            " ");

        var localizer = CreateLocalizer(customLocalizer);

        var result = localizer["WhitespaceKey"];

        Assert.Equal("WhitespaceKey", result);
    }

    private static InternalQuickGridLocalizer CreateLocalizer(QuickGridLocalizer? customLocalizer = null)
    {
        var resourceManager = new ResourceManager(
            "Microsoft.AspNetCore.Components.QuickGrid.Resources.QuickGridLocalization",
            typeof(Paginator).Assembly);

        return new InternalQuickGridLocalizer(resourceManager, customLocalizer);
    }

    private sealed class TestQuickGridLocalizer : QuickGridLocalizer
    {
        private readonly string? _key;
        private readonly string? _value;

        public TestQuickGridLocalizer()
        {
        }

        public TestQuickGridLocalizer(string key, string value)
        {
            _key = key;
            _value = value;
        }

        public override LocalizedString this[string key]
        {
            get
            {
                if (string.Equals(_key, key, StringComparison.Ordinal))
                {
                    return new LocalizedString(key, _value!, resourceNotFound: false);
                }

                return new LocalizedString(key, key, resourceNotFound: true);
            }
        }

        public override LocalizedString this[string key, params object[] arguments]
        {
            get
            {
                var localizedString = this[key];

                if (localizedString.ResourceNotFound)
                {
                    return localizedString;
                }

                var formattedValue = string.Format(
                    System.Globalization.CultureInfo.CurrentCulture,
                    localizedString.Value,
                    arguments);

                return new LocalizedString(key, formattedValue, resourceNotFound: false);
            }
        }
    }
}
