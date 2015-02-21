// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.Framework.Internal;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class BindingSourceModelBinderTest
    {
        [Fact]
        public void BindingSourceModelBinder_ThrowsOnNonGreedySource()
        {
            // Arrange
            var expected =
                "The provided binding source 'Test Source' is not a greedy data source. " +
                "'BindingSourceModelBinder' only supports greedy data sources." + Environment.NewLine +
                "Parameter name: bindingSource";

            var bindingSource = new BindingSource(
                "Test",
                displayName: "Test Source",
                isGreedy: false,
                isFromRequest: true);

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(
                () => new TestableBindingSourceModelBinder(bindingSource));
            Assert.Equal(expected, exception.Message);
        }

        [Fact]
        public async Task BindingSourceModelBinder_ReturnsFalse_WithNoSource()
        {
            // Arrange
            var context = new ModelBindingContext();
            context.ModelMetadata = new EmptyModelMetadataProvider().GetMetadataForType(
                modelAccessor: null,
                modelType: typeof(string));

            var binder = new TestableBindingSourceModelBinder(BindingSource.Body);

            // Act 
            var result = await binder.BindModelAsync(context);

            // Assert
            Assert.Null(result);
            Assert.False(binder.WasBindModelCoreCalled);
        }

        [Fact]
        public async Task BindingSourceModelBinder_ReturnsFalse_NonMatchingSource()
        {
            // Arrange
            var context = new ModelBindingContext();
            context.ModelMetadata = new EmptyModelMetadataProvider().GetMetadataForType(
                modelAccessor: null,
                modelType: typeof(string));

            context.ModelMetadata.BinderMetadata = new ModelBinderAttribute()
            {
                BindingSource = BindingSource.Query,
            };

            var binder = new TestableBindingSourceModelBinder(BindingSource.Body);

            // Act 
            var result = await binder.BindModelAsync(context);

            // Assert
            Assert.Null(result);
            Assert.False(binder.WasBindModelCoreCalled);
        }

        [Fact]
        public async Task BindingSourceModelBinder_ReturnsTrue_MatchingSource()
        {
            // Arrange
            var context = new ModelBindingContext();
            context.ModelMetadata = new EmptyModelMetadataProvider().GetMetadataForType(
                modelAccessor: null,
                modelType: typeof(string));

            context.ModelMetadata.BinderMetadata = new ModelBinderAttribute()
            {
                BindingSource = BindingSource.Body,
            };

            var binder = new TestableBindingSourceModelBinder(BindingSource.Body);

            // Act 
            var result = await binder.BindModelAsync(context);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsModelSet);
            Assert.True(binder.WasBindModelCoreCalled);
        }

        private class TestableBindingSourceModelBinder : BindingSourceModelBinder
        {
            public bool WasBindModelCoreCalled { get; private set; }

            public TestableBindingSourceModelBinder(BindingSource source)
                : base(source)
            {
            }

            protected override Task<ModelBindingResult> BindModelCoreAsync([NotNull] ModelBindingContext bindingContext)
            {
                WasBindModelCoreCalled = true;
                return Task.FromResult(new ModelBindingResult(null, bindingContext.ModelName, true));
            }
        }
    }
}