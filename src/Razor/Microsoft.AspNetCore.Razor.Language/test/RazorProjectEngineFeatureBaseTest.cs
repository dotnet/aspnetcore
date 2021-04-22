// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language
{
    public class RazorProjectEngineFeatureBaseTest
    {
        [Fact]
        public void ProjectEngineSetter_CallsOnInitialized()
        {
            // Arrange
            var testFeature = new TestFeature();

            // Act
            testFeature.ProjectEngine = Mock.Of<RazorProjectEngine>();

            // Assert
            Assert.Equal(1, testFeature.InitializationCount);
        }

        private class TestFeature : RazorProjectEngineFeatureBase
        {
            public int InitializationCount { get; private set; }

            protected override void OnInitialized()
            {
                InitializationCount++;
            }
        }
    }
}
