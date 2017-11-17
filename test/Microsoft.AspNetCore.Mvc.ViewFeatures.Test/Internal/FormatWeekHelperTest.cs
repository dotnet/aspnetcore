// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Internal
{
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
            var cache = new DefaultMetadataDetails(key, new ModelAttributes(new object[0], null, null));

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
}
