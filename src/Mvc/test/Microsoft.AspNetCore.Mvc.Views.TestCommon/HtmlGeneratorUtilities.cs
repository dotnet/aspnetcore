// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.WebEncoders.Testing;
using Moq;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures
{
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
}
