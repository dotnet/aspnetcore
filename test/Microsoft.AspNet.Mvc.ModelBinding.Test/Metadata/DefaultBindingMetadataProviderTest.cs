// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding.Metadata
{
    public class DefaultModelMetadataBindingDetailsProviderTest
    {
        [Fact]
        public void GetBindingDetails_FindsBinderTypeProvider()
        {
            // Arrange
            var attributes = new object[]
            {
                new ModelBinderAttribute() { BinderType = typeof(HeaderModelBinder) },
                new ModelBinderAttribute() { BinderType = typeof(ArrayModelBinder<string>) },
            };

            var context = new BindingMetadataProviderContext(
                ModelMetadataIdentity.ForType(typeof(string)), 
                attributes);

            var provider = new DefaultBindingMetadataProvider();

            // Act
            provider.GetBindingMetadata(context);

            // Assert
            Assert.Equal(typeof(HeaderModelBinder), context.BindingMetadata.BinderType);
        }

        [Fact]
        public void GetBindingDetails_FindsBinderTypeProvider_IfNullFallsBack()
        {
            // Arrange
            var attributes = new object[]
            {
                new ModelBinderAttribute(),
                new ModelBinderAttribute() { BinderType = typeof(HeaderModelBinder) },
                new ModelBinderAttribute() { BinderType = typeof(ArrayModelBinder<string>) },
            };

            var context = new BindingMetadataProviderContext(
                ModelMetadataIdentity.ForType(typeof(string)),
                attributes);

            var provider = new DefaultBindingMetadataProvider();

            // Act
            provider.GetBindingMetadata(context);

            // Assert
            Assert.Equal(typeof(HeaderModelBinder), context.BindingMetadata.BinderType);
        }

        [Fact]
        public void GetBindingDetails_FindsModelName()
        {
            // Arrange
            var attributes = new object[]
            {
                new ModelBinderAttribute() { Name = "Product" },
                new ModelBinderAttribute() { Name = "Order" },
            };

            var context = new BindingMetadataProviderContext(
                ModelMetadataIdentity.ForType(typeof(string)),
                attributes);

            var provider = new DefaultBindingMetadataProvider();

            // Act
            provider.GetBindingMetadata(context);

            // Assert
            Assert.Equal("Product", context.BindingMetadata.BinderModelName);
        }

        [Fact]
        public void GetBindingDetails_FindsModelName_IfEmpty()
        {
            // Arrange
            var attributes = new object[]
            {
                new ModelBinderAttribute(),
                new ModelBinderAttribute() { Name = "Product" },
                new ModelBinderAttribute() { Name = "Order" },
            };

            var context = new BindingMetadataProviderContext(
                ModelMetadataIdentity.ForType(typeof(string)),
                attributes);

            var provider = new DefaultBindingMetadataProvider();

            // Act
            provider.GetBindingMetadata(context);

            // Assert
            Assert.Null(context.BindingMetadata.BinderModelName);
        }

        [Fact]
        public void GetBindingDetails_FindsBindingSource()
        {
            // Arrange
            var attributes = new object[]
            {
                new ModelBinderAttribute() { BindingSource = BindingSource.Body },
                new ModelBinderAttribute() { BindingSource = BindingSource.Query },
            };

            var context = new BindingMetadataProviderContext(
                ModelMetadataIdentity.ForType(typeof(string)),
                attributes);

            var provider = new DefaultBindingMetadataProvider();

            // Act
            provider.GetBindingMetadata(context);

            // Assert
            Assert.Equal(BindingSource.Body, context.BindingMetadata.BindingSource);
        }

        [Fact]
        public void GetBindingDetails_FindsBindingSource_IfNullFallsBack()
        {
            // Arrange
            var attributes = new object[]
            {
                new ModelBinderAttribute(),
                new ModelBinderAttribute() { BindingSource = BindingSource.Body },
                new ModelBinderAttribute() { BindingSource = BindingSource.Query },
            };

            var context = new BindingMetadataProviderContext(
                ModelMetadataIdentity.ForType(typeof(string)),
                attributes);

            var provider = new DefaultBindingMetadataProvider();

            // Act
            provider.GetBindingMetadata(context);

            // Assert
            Assert.Equal(BindingSource.Body, context.BindingMetadata.BindingSource);
        }
    }
}