// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Moq;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures;

/// <summary>
/// Extension methods for <see cref="IAntiforgery"/>.
/// </summary>
public class AntiforgeryExtensionsTest
{
    [Fact]
    public void GetHtml_RendersInputField()
    {
        // Arrange
        var antiforgery = new Mock<IAntiforgery>(MockBehavior.Strict);
        var tokenSet = new AntiforgeryTokenSet("request-token", "cookie-token", "form-field", "header");
        antiforgery
            .Setup(a => a.GetAndStoreTokens(It.IsAny<HttpContext>()))
            .Returns(tokenSet);

        // Act
        var inputElement = AntiforgeryExtensions.GetHtml(antiforgery.Object, new DefaultHttpContext());

        // Assert
        Assert.Equal(
            @"<input name=""HtmlEncode[[form-field]]"" type=""hidden"" value=""HtmlEncode[[request-token]]"" />",
            HtmlContentUtilities.HtmlContentToString(inputElement));
    }
}
