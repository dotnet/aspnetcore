// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;
using Xunit;

namespace Polly;

public class HttpRequestMessageExtensionsTest
{
    [Fact]
    public void GetPolicyExecutionContext_Found_SetsContext()
    {
        // Arrange
        var request = new HttpRequestMessage();
        var expected = new Context(Guid.NewGuid().ToString());
#if USE_OBSOLETED
        request.Properties[HttpRequestMessageExtensions.PolicyExecutionContextKey] = expected;
#else
        request.Options.Set(new HttpRequestOptionsKey<Context>(HttpRequestMessageExtensions.PolicyExecutionContextKey), expected);
#endif

        // Act
        var actual = request.GetPolicyExecutionContext();

        // Assert
        Assert.Same(expected, actual);
    }

    [Fact]
    public void GetPolicyExecutionContext_NotFound_ReturnsNull()
    {
        // Arrange
        var request = new HttpRequestMessage();

        // Act
        var actual = request.GetPolicyExecutionContext();

        // Assert
        Assert.Null(actual);
    }

    [Fact]
    public void GetPolicyExecutionContext_Null_ReturnsNull()
    {
        // Arrange
        var request = new HttpRequestMessage();
#if USE_OBSOLETED
        request.Properties[HttpRequestMessageExtensions.PolicyExecutionContextKey] = null;
#else
        request.Options.Set(new HttpRequestOptionsKey<Context>(HttpRequestMessageExtensions.PolicyExecutionContextKey), null);
#endif

        // Act
        var actual = request.GetPolicyExecutionContext();

        // Assert
        Assert.Null(actual);
    }

    [Fact]
    public void SetPolicyExecutionContext_WithValue_SetsContext()
    {
        // Arrange
        var request = new HttpRequestMessage();
        var expected = new Context(Guid.NewGuid().ToString());

        // Act
        request.SetPolicyExecutionContext(expected);

        // Assert
#if USE_OBSOLETED
        var actual = request.Properties[HttpRequestMessageExtensions.PolicyExecutionContextKey];
#else
        request.Options.TryGetValue(
            new HttpRequestOptionsKey<Context>(HttpRequestMessageExtensions.PolicyExecutionContextKey),
            out Context actual);
#endif
        Assert.Same(expected, actual);
    }

    [Fact]
    public void SetPolicyExecutionContext_WithNull_SetsNull()
    {
        // Arrange
        var request = new HttpRequestMessage();
#if USE_OBSOLETED
        request.Properties[HttpRequestMessageExtensions.PolicyExecutionContextKey] = new Context(Guid.NewGuid().ToString());
#else
        request.Options.Set(new HttpRequestOptionsKey<Context>(HttpRequestMessageExtensions.PolicyExecutionContextKey), new Context(Guid.NewGuid().ToString()));
#endif

        // Act
        request.SetPolicyExecutionContext(null);

        // Assert
#if USE_OBSOLETED
        var actual = request.Properties[HttpRequestMessageExtensions.PolicyExecutionContextKey];
#else
        request.Options.TryGetValue(
            new HttpRequestOptionsKey<Context>(HttpRequestMessageExtensions.PolicyExecutionContextKey),
            out Context actual);
#endif
        Assert.Null(actual);
    }
}
