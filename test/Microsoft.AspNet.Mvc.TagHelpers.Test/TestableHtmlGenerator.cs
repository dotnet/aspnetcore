// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNet.Antiforgery;
using Microsoft.AspNet.Html.Abstractions;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.Mvc.Abstractions;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Mvc.Routing;
using Microsoft.AspNet.Mvc.ViewEngines;
using Microsoft.AspNet.Mvc.ViewFeatures;
using Microsoft.AspNet.Routing;
using Microsoft.Extensions.OptionsModel;
using Microsoft.Extensions.WebEncoders.Testing;
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
            IOptions<MvcViewOptions> options,
            IUrlHelper urlHelper,
            IDictionary<string, object> validationAttributes)
            : base(Mock.Of<IAntiforgery>(), options, metadataProvider, urlHelper, new HtmlTestEncoder())
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
                TextWriter.Null,
                new HtmlHelperOptions());

            return viewContext;
        }

        public override IHtmlContent GenerateAntiforgery(ViewContext viewContext)
        {
            var tagBuilder = new TagBuilder("input")
            {
                Attributes =
                {
                    { "name", "__RequestVerificationToken" },
                    { "type", "hidden" },
                    { "value", "olJlUDjrouRNWLen4tQJhauj1Z1rrvnb3QD65cmQU1Ykqi6S4" }, // 50 chars of a token.
                },
            };

            tagBuilder.TagRenderMode = TagRenderMode.SelfClosing;
            return tagBuilder;
        }

        protected override IDictionary<string, object> GetValidationAttributes(
            ViewContext viewContext,
            ModelExplorer modelExplorer,
            string name)
        {
            return ValidationAttributes;
        }

        private static IOptions<MvcViewOptions> GetOptions()
        {
            var mockOptions = new Mock<IOptions<MvcViewOptions>>();
            mockOptions
                .SetupGet(options => options.Value)
                .Returns(new MvcViewOptions());

            return mockOptions.Object;
        }
    }
}