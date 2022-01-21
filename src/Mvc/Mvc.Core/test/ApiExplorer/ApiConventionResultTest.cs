// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ApiExplorer;

public class ApiConventionResultTest
{
    [Fact]
    public void GetApiConvention_ReturnsNull_IfNoConventionMatches()
    {
        // Arrange
        var method = typeof(GetApiConvention_ReturnsNull_IfNoConventionMatchesController).GetMethod(nameof(GetApiConvention_ReturnsNull_IfNoConventionMatchesController.NoMatch));
        var attribute = new ApiConventionTypeAttribute(typeof(DefaultApiConventions));

        // Act
        var result = ApiConventionResult.TryGetApiConvention(method, new[] { attribute }, out var conventionResult);

        // Assert
        Assert.False(result);
        Assert.Null(conventionResult);
    }

    public class GetApiConvention_ReturnsNull_IfNoConventionMatchesController
    {
        public IActionResult NoMatch(int id) => null;
    }

    [Fact]
    public void GetApiConvention_ReturnsResultFromConvention()
    {
        // Arrange
        var method = typeof(GetApiConvention_ReturnsResultFromConventionController)
            .GetMethod(nameof(GetApiConvention_ReturnsResultFromConventionController.Match));
        var attribute = new ApiConventionTypeAttribute(typeof(GetApiConvention_ReturnsResultFromConventionType));

        // Act
        var result = ApiConventionResult.TryGetApiConvention(method, new[] { attribute }, out var conventionResult);

        // Assert
        Assert.True(result);
        Assert.Collection(
            conventionResult.ResponseMetadataProviders.OrderBy(o => o.StatusCode),
            r => Assert.Equal(201, r.StatusCode),
            r => Assert.Equal(403, r.StatusCode));
    }

    public class GetApiConvention_ReturnsResultFromConventionController
    {
        public IActionResult Match(int id) => null;
    }

    public static class GetApiConvention_ReturnsResultFromConventionType
    {
        [ProducesResponseType(200)]
        [ProducesResponseType(202)]
        [ProducesResponseType(404)]
        public static void Get(int id) { }

        [ProducesResponseType(201)]
        [ProducesResponseType(403)]
        public static void Match(int id) { }
    }

    [Fact]
    public void GetApiConvention_ReturnsResultFromFirstMatchingConvention()
    {
        // Arrange
        var method = typeof(GetApiConvention_ReturnsResultFromFirstMatchingConventionController)
            .GetMethod(nameof(GetApiConvention_ReturnsResultFromFirstMatchingConventionController.Get));
        var attributes = new[]
        {
                new ApiConventionTypeAttribute(typeof(GetApiConvention_ReturnsResultFromConventionType)),
                new ApiConventionTypeAttribute(typeof(DefaultApiConventions)),
            };

        // Act
        var result = ApiConventionResult.TryGetApiConvention(method, attributes, result: out var conventionResult);

        // Assert
        Assert.True(result);
        Assert.Collection(
            conventionResult.ResponseMetadataProviders.OrderBy(o => o.StatusCode),
            r => Assert.Equal(200, r.StatusCode),
            r => Assert.Equal(202, r.StatusCode),
            r => Assert.Equal(404, r.StatusCode));
    }

    public class GetApiConvention_ReturnsResultFromFirstMatchingConventionController
    {
        public IActionResult Get(int id) => null;
    }

    [Fact]
    public void GetApiConvention_GetAction_MatchesDefaultConvention()
    {
        // Arrange
        var method = typeof(DefaultConventionController)
            .GetMethod(nameof(DefaultConventionController.GetUser));
        var attributes = new[] { new ApiConventionTypeAttribute(typeof(DefaultApiConventions)) };

        // Act
        var result = ApiConventionResult.TryGetApiConvention(method, attributes, out var conventionResult);

        // Assert
        Assert.True(result);
        Assert.Collection(
            conventionResult.ResponseMetadataProviders.OrderBy(o => o.StatusCode),
            r => Assert.IsAssignableFrom<IApiDefaultResponseMetadataProvider>(r),
            r => Assert.Equal(200, r.StatusCode),
            r => Assert.Equal(404, r.StatusCode));
    }

    [Fact]
    public void GetApiConvention_PostAction_MatchesDefaultConvention()
    {
        // Arrange
        var method = typeof(DefaultConventionController)
            .GetMethod(nameof(DefaultConventionController.PostUser));
        var attributes = new[] { new ApiConventionTypeAttribute(typeof(DefaultApiConventions)) };

        // Act
        var result = ApiConventionResult.TryGetApiConvention(method, attributes, out var conventionResult);

        // Assert
        Assert.True(result);
        Assert.Collection(
            conventionResult.ResponseMetadataProviders.OrderBy(o => o.StatusCode),
            r => Assert.IsAssignableFrom<IApiDefaultResponseMetadataProvider>(r),
            r => Assert.Equal(201, r.StatusCode),
            r => Assert.Equal(400, r.StatusCode));
    }

    [Fact]
    public void GetApiConvention_PutAction_MatchesDefaultConvention()
    {
        // Arrange
        var method = typeof(DefaultConventionController)
            .GetMethod(nameof(DefaultConventionController.PutUser));
        var conventions = new[]
        {
                new ApiConventionTypeAttribute(typeof(DefaultApiConventions)),
            };

        // Act
        var result = ApiConventionResult.TryGetApiConvention(method, conventions, out var conventionResult);

        // Assert
        Assert.True(result);
        Assert.Collection(
            conventionResult.ResponseMetadataProviders.OrderBy(o => o.StatusCode),
            r => Assert.IsAssignableFrom<IApiDefaultResponseMetadataProvider>(r),
            r => Assert.Equal(204, r.StatusCode),
            r => Assert.Equal(400, r.StatusCode),
            r => Assert.Equal(404, r.StatusCode));
    }

    [Fact]
    public void GetApiConvention_DeleteAction_MatchesDefaultConvention()
    {
        // Arrange
        var method = typeof(DefaultConventionController)
            .GetMethod(nameof(DefaultConventionController.Delete));
        var conventions = new[]
        {
                new ApiConventionTypeAttribute(typeof(DefaultApiConventions)),
            };

        // Act
        var result = ApiConventionResult.TryGetApiConvention(method, conventions, out var conventionResult);

        // Assert
        Assert.True(result);
        Assert.Collection(
            conventionResult.ResponseMetadataProviders.OrderBy(o => o.StatusCode),
            r => Assert.IsAssignableFrom<IApiDefaultResponseMetadataProvider>(r),
            r => Assert.Equal(200, r.StatusCode),
            r => Assert.Equal(400, r.StatusCode),
            r => Assert.Equal(404, r.StatusCode));
    }

    [Fact]
    public void GetApiConvention_UsesApiConventionMethod()
    {
        // Arrange
        var method = typeof(DefaultConventionController)
            .GetMethod(nameof(DefaultConventionController.EditUser));
        var conventions = new[]
        {
                new ApiConventionTypeAttribute(typeof(DefaultApiConventions)),
            };

        // Act
        var result = ApiConventionResult.TryGetApiConvention(method, conventions, out var conventionResult);

        // Assert
        Assert.True(result);
        Assert.Collection(
            conventionResult.ResponseMetadataProviders.OrderBy(o => o.StatusCode),
            r => Assert.IsAssignableFrom<IApiDefaultResponseMetadataProvider>(r),
            r => Assert.Equal(201, r.StatusCode),
            r => Assert.Equal(400, r.StatusCode));
    }

    public class DefaultConventionController
    {
        public IActionResult GetUser(Guid id) => null;

        public IActionResult PostUser(User user) => null;

        public IActionResult PutUser(Guid userId, User user) => null;

        public IActionResult Delete(Guid userId) => null;

        [ApiConventionMethod(typeof(DefaultApiConventions), nameof(DefaultApiConventions.Post))]
        public IActionResult EditUser(int id, User user) => null;
    }

    public class User { }
}
