// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures;

public class FormatWeekHelperTest
{
    // See blog post: https://blogs.msdn.microsoft.com/shawnste/2006/01/24/iso-8601-week-of-year-format-in-microsoft-net/
    [Theory]
    [InlineData(2001, 1, 1, "2001-W01")]
    [InlineData(2007, 12, 31, "2008-W01")]
    [InlineData(2000, 12, 31, "2000-W52")]
    [InlineData(2012, 1, 1, "2011-W52")]
    [InlineData(2005, 1, 1, "2004-W53")]
    [InlineData(2015, 12, 31, "2015-W53")]
    public void GetFormattedWeek_ReturnsExpectedFormattedValue(int year, int month, int day, string expectedOutput)
    {
        // Arrange
        var detailsProvider = new DefaultCompositeMetadataDetailsProvider(
            Enumerable.Empty<IMetadataDetailsProvider>());
        var key = ModelMetadataIdentity.ForType(typeof(DateTime));
        var cache = new DefaultMetadataDetails(key, new ModelAttributes(
            Array.Empty<object>(),
            Array.Empty<object>(),
            Array.Empty<object>()));

        var provider = new EmptyModelMetadataProvider();
        var metadata = new DefaultModelMetadata(provider, detailsProvider, cache);
        var model = new DateTime(year, month, day);
        var modelExplorer = new ModelExplorer(provider, metadata, model);

        // Act
        var formattedValue = FormatWeekHelper.GetFormattedWeek(modelExplorer);

        // Assert
        Assert.Equal(expectedOutput, formattedValue);
    }
}
