// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Resources;
using Xunit;

namespace Microsoft.AspNetCore.Components.QuickGrid.Tests;

public class LocalizationTests
{
    [Fact]
    public void DefaultInterceptor_UsesResxAndFormats()
    {
        var rm = new ResourceManager(typeof(QuickGridResources));
        var interceptor = new DefaultQuickGridLocalizationInterceptor(rm);

        var result = interceptor.Handle("QuickGridPaginatorPageSummary", new object[] { 2, 5 });

        Assert.False(result.ResourceNotFound);
        Assert.Equal("Page 2 of 5", result.Value);
    }

    [Fact]
    public void DefaultInterceptor_UsesCustomLocalizerWhenAvailable()
    {
        var rm = new ResourceManager(typeof(QuickGridResources));
        var custom = new TestQuickGridLocalizer();
        var interceptor = new DefaultQuickGridLocalizationInterceptor(rm, custom);

        var result = interceptor.Handle("QuickGridPaginatorPageSummary", new object[] { 3, 7 });

        Assert.False(result.ResourceNotFound);
        Assert.Equal("Page 3 sur 7", result.Value);
    }

    [Fact]
    public void DefaultInterceptor_MissingKeyReturnsKeyMarkedNotFound()
    {
        var rm = new ResourceManager(typeof(QuickGridResources));
        var interceptor = new DefaultQuickGridLocalizationInterceptor(rm);

        var result = interceptor.Handle("NoSuchKey", null);

        Assert.True(result.ResourceNotFound);
        Assert.Equal("NoSuchKey", result.Value);
    }

    private class TestQuickGridLocalizer : QuickGridLocalizer
    {
        public override QuickGridLocalizedString this[string key]
        {
            get
            {
                if (key == "QuickGridPaginatorPageSummary")
                {
                    return new QuickGridLocalizedString(key, "Page {0} sur {1}", resourceNotFound: false);
                }

                return base[key];
            }
        }
    }
}
