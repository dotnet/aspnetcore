// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures
{
    public class ViewDataOfTTest
    {
        [Fact]
        public void SettingModelThrowsIfTheModelIsNull()
        {
            // Arrange
            var viewDataOfT = new ViewDataDictionary<int>(new EmptyModelMetadataProvider());
            ViewDataDictionary viewData = viewDataOfT;

            // Act and Assert
            Exception ex = Assert.Throws<InvalidOperationException>(() => viewData.Model = null);
            Assert.Equal("The model item passed is null, but this ViewDataDictionary instance requires a non-null model item of type 'System.Int32'.", ex.Message);
        }

        [Fact]
        public void SettingModelThrowsIfTheModelIsIncompatible()
        {
            // Arrange
            var viewDataOfT = new ViewDataDictionary<string>(new EmptyModelMetadataProvider());
            ViewDataDictionary viewData = viewDataOfT;

            // Act and Assert
            Exception ex = Assert.Throws<InvalidOperationException>(() => viewData.Model = DateTime.UtcNow);
            Assert.Equal("The model item passed into the ViewDataDictionary is of type 'System.DateTime', but this ViewDataDictionary instance requires a model item of type 'System.String'.", ex.Message);
        }

        [Fact]
        public void SettingModelWorksForCompatibleTypes()
        {
            // Arrange
            var value = "some value";
            var viewDataOfT = new ViewDataDictionary<object>(new EmptyModelMetadataProvider());
            ViewDataDictionary viewData = viewDataOfT;

            // Act
            viewData.Model = value;

            // Assert
            Assert.Same(value, viewDataOfT.Model);
        }

        [Fact]
        public void PropertiesInitializedCorrectly()
        {
            // Arrange
            var viewData = new ViewDataDictionary<string>(new EmptyModelMetadataProvider());

            // Act & Assert
            Assert.Empty(viewData);
            Assert.Empty(viewData);
            Assert.False(viewData.IsReadOnly);

            Assert.NotNull(viewData.Keys);
            Assert.Empty(viewData.Keys);

            Assert.Null(viewData.Model);
            Assert.NotNull(viewData.ModelMetadata);
            Assert.NotNull(viewData.ModelState);

            Assert.NotNull(viewData.TemplateInfo);
            Assert.Equal(0, viewData.TemplateInfo.TemplateDepth);
            Assert.Equal(string.Empty, viewData.TemplateInfo.FormattedModelValue);
            Assert.Equal(string.Empty, viewData.TemplateInfo.HtmlFieldPrefix);

            Assert.NotNull(viewData.Values);
            Assert.Empty(viewData.Values);
        }

        [Fact]
        public void TemplateInfoPropertiesAreNeverNull()
        {
            // Arrange
            var viewData = new ViewDataDictionary<string>(new EmptyModelMetadataProvider());

            // Act
            viewData.TemplateInfo.FormattedModelValue = null;
            viewData.TemplateInfo.HtmlFieldPrefix = null;

            // Assert
            Assert.Equal(string.Empty, viewData.TemplateInfo.FormattedModelValue);
            Assert.Equal(string.Empty, viewData.TemplateInfo.HtmlFieldPrefix);
        }
    }
}
