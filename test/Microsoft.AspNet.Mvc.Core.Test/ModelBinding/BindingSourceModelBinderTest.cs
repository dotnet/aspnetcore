// Copyright (c) .NET Foundation. All rights reserved.
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
                () => new TestableBindingSourceModelBinder(bindingSource, isModelSet: false));
            Assert.Equal(expected, exception.Message);
        }

        [Fact]
        public async Task BindingSourceModelBinder_ReturnsNull_WithNoSource()
        {
            // Arrange
            var context = new ModelBindingContext();
            context.ModelMetadata = new EmptyModelMetadataProvider().GetMetadataForType(typeof(string));

            var binder = new TestableBindingSourceModelBinder(BindingSource.Body, isModelSet: false);

            // Act
            var result = await binder.BindModelAsync(context);

            // Assert
            Assert.Null(result);
            Assert.False(binder.WasBindModelCoreCalled);
        }

        [Fact]
        public async Task BindingSourceModelBinder_ReturnsNull_NonMatchingSource()
        {
            // Arrange
            var provider = new TestModelMetadataProvider();
            provider.ForType<string>().BindingDetails(d => d.BindingSource = BindingSource.Query);

            var context = new ModelBindingContext();
            context.ModelMetadata = provider.GetMetadataForType(typeof(string));

            var binder = new TestableBindingSourceModelBinder(BindingSource.Body, isModelSet: false);

            // Act
            var result = await binder.BindModelAsync(context);

            // Assert
            Assert.Null(result);
            Assert.False(binder.WasBindModelCoreCalled);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task BindingSourceModelBinder_ReturnsNonNull_MatchingSource(bool isModelSet)
        {
            // Arrange
            var provider = new TestModelMetadataProvider();
            provider.ForType<string>().BindingDetails(d => d.BindingSource = BindingSource.Body);
            var modelMetadata = provider.GetMetadataForType(typeof(string));
            var context = new ModelBindingContext()
            {
                ModelMetadata = modelMetadata,
                BindingSource = modelMetadata.BindingSource,
                BinderModelName = modelMetadata.BinderModelName
            };

            var binder = new TestableBindingSourceModelBinder(BindingSource.Body, isModelSet);

            // Act
            var result = await binder.BindModelAsync(context);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(isModelSet, result.IsModelSet);
            Assert.Null(result.Model);
            Assert.True(binder.WasBindModelCoreCalled);
        }

        private class TestableBindingSourceModelBinder : BindingSourceModelBinder
        {
            bool _isModelSet;

            public TestableBindingSourceModelBinder(BindingSource source, bool isModelSet)
                : base(source)
            {
                _isModelSet = isModelSet;
            }

            public bool WasBindModelCoreCalled { get; private set; }

            protected override Task<ModelBindingResult> BindModelCoreAsync([NotNull] ModelBindingContext bindingContext)
            {
                WasBindModelCoreCalled = true;
                return Task.FromResult(
                    new ModelBindingResult(model: null, key: bindingContext.ModelName, isModelSet: _isModelSet));
            }
        }
    }
}