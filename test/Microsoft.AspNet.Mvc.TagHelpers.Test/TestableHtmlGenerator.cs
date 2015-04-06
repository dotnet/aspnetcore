// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNet.DataProtection;
using Microsoft.AspNet.Http.Core;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Routing;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.OptionsModel;
using Microsoft.Framework.WebEncoders;
using Moq;

namespace Microsoft.AspNet.Mvc.TagHelpers
{
    public class TestableHtmlGenerator : DefaultHtmlGenerator
    {
        private IDictionary<string, object> _validationAttributes;

        public TestableHtmlGenerator(IModelMetadataProvider metadataProvider)
            : this(metadataProvider, Mock.Of<IUrlHelper>())
        {
        }

        public TestableHtmlGenerator(IModelMetadataProvider metadataProvider, IUrlHelper urlHelper)
            : this(
                  metadataProvider,
                  GetOptions(),
                  urlHelper,
                  validationAttributes: new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase))
        {
        }

        public TestableHtmlGenerator(
            IModelMetadataProvider metadataProvider,
            IOptions<MvcOptions> options,
            IUrlHelper urlHelper,
            IDictionary<string, object> validationAttributes)
            : base(GetAntiForgery(), options, metadataProvider, urlHelper, new HtmlEncoder())
        {
            _validationAttributes = validationAttributes;
        }

        public IDictionary<string, object> ValidationAttributes
        {
            get { return _validationAttributes; }
        }

        public static ViewContext GetViewContext(
            object model,
            IHtmlGenerator htmlGenerator,
            IModelMetadataProvider metadataProvider)
        {
            var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());
            var viewData = new ViewDataDictionary(metadataProvider)
            {
                Model = model,
            };
            var viewContext = new ViewContext(
                actionContext,
                Mock.Of<IView>(),
                viewData,
                Mock.Of<ITempDataDictionary>(),
                TextWriter.Null);

            return viewContext;
        }

        public override TagBuilder GenerateAntiForgery(ViewContext viewContext)
        {
            return new TagBuilder("input", new HtmlEncoder())
            {
                Attributes =
                {
                    { "name", "__RequestVerificationToken" },
                    { "type", "hidden" },
                    { "value", "olJlUDjrouRNWLen4tQJhauj1Z1rrvnb3QD65cmQU1Ykqi6S4" }, // 50 chars of a token.
                },
            };
        }

        protected override IDictionary<string, object> GetValidationAttributes(
            ViewContext viewContext,
            ModelExplorer modelExplorer,
            string name)
        {
            return ValidationAttributes;
        }

        private static IOptions<MvcOptions> GetOptions()
        {
            var mockOptions = new Mock<IOptions<MvcOptions>>();
            mockOptions
                .SetupGet(options => options.Options)
                .Returns(new MvcOptions());

            return mockOptions.Object;
        }
        private static AntiForgery GetAntiForgery()
        {
            // AntiForgery must be passed to TestableHtmlGenerator constructor but will never be called.
            var optionsAccessor = new Mock<IOptions<MvcOptions>>();
            var mockDataProtectionOptions = new Mock<IOptions<DataProtectionOptions>>();
            mockDataProtectionOptions
                .SetupGet(options => options.Options)
                .Returns(Mock.Of<DataProtectionOptions>());
            optionsAccessor.SetupGet(o => o.Options).Returns(new MvcOptions());
            optionsAccessor
                .SetupGet(o => o.Options)
                .Returns(new MvcOptions());
            var antiForgery = new AntiForgery(
                Mock.Of<IClaimUidExtractor>(),
                Mock.Of<IDataProtectionProvider>(),
                Mock.Of<IAntiForgeryAdditionalDataProvider>(),
                optionsAccessor.Object,
                new HtmlEncoder(),
                mockDataProtectionOptions.Object);

            return antiForgery;
        }
    }
}