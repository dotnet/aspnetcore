// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.ViewCompilation
{
    public class ApplicationConsumingPrecompiledViews
        : IClassFixture<ApplicationConsumingPrecompiledViews.ApplicationConsumingPrecompiledViewsFixture>
    {
        public ApplicationConsumingPrecompiledViews(ApplicationConsumingPrecompiledViewsFixture fixture)
        {
            Fixture = fixture;
        }

        public ApplicationTestFixture Fixture { get; }

        [Fact]
        public async Task ConsumingClassLibrariesWithPrecompiledViewsWork()
        {
            // Arrange
            var deploymentResult = Fixture.CreateDeployment();

            // Act
            var response = await Fixture.HttpClient.GetStringWithRetryAsync(
                deploymentResult.ApplicationBaseUri + "Manage/Home",
                Fixture.Logger);

            // Assert
            TestEmbeddedResource.AssertContent("ApplicationConsumingPrecompiledViews.Manage.Home.Index.txt", response);
        }

        public class ApplicationConsumingPrecompiledViewsFixture : ApplicationTestFixture
        {
            public ApplicationConsumingPrecompiledViewsFixture()
                : base("ApplicationUsingPrecompiledViewClassLibrary")
            {
            }
        }
    }
}
