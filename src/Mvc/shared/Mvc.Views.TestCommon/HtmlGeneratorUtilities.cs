// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.WebEncoders.Testing;
using Moq;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures;

public static class HtmlGeneratorUtilities
{
    public static IHtmlGenerator GetHtmlGenerator(IModelMetadataProvider provider)
    {
        var options = new MvcViewOptions();
        var urlHelperFactory = new Mock<IUrlHelperFactory>();
        urlHelperFactory
            .Setup(f => f.GetUrlHelper(It.IsAny<ActionContext>()))
            .Returns(Mock.Of<IUrlHelper>());

        return GetHtmlGenerator(provider, urlHelperFactory.Object, options);
    }

    public static IHtmlGenerator GetHtmlGenerator(IModelMetadataProvider provider, IUrlHelperFactory urlHelperFactory, MvcViewOptions options)
    {
        var optionsAccessor = new Mock<IOptions<MvcViewOptions>>();
        optionsAccessor
            .SetupGet(o => o.Value)
            .Returns(options);

        var attributeProvider = new DefaultValidationHtmlAttributeProvider(
            optionsAccessor.Object,
            provider,
            new ClientValidatorCache());

        var htmlGenerator = new DefaultHtmlGenerator(
                Mock.Of<IAntiforgery>(),
                optionsAccessor.Object,
                provider,
                urlHelperFactory,
                new HtmlTestEncoder(),
                attributeProvider);
        return htmlGenerator;
    }
}
