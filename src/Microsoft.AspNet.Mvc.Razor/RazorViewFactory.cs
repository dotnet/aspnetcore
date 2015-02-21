// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.Razor
{
    /// <summary>
    /// Represents the default <see cref="IRazorViewFactory"/> implementation that creates
    /// <see cref="RazorView"/> instances with a given <see cref="IRazorPage"/>.
    /// </summary>
    public class RazorViewFactory : IRazorViewFactory
    {
        private readonly IRazorPageActivator _pageActivator;
        private readonly IViewStartProvider _viewStartProvider;

        /// <summary>
        /// Initializes a new instance of RazorViewFactory
        /// </summary>
        /// <param name="pageActivator">The <see cref="IRazorPageActivator"/> used to activate pages.</param>
        /// <param name="viewStartProvider">The <see cref="IViewStartProvider"/> used for discovery of _ViewStart
        /// pages</param>
        public RazorViewFactory(IRazorPageActivator pageActivator,
                                IViewStartProvider viewStartProvider)
        {
            _pageActivator = pageActivator;
            _viewStartProvider = viewStartProvider;
        }

        /// <inheritdoc />
        public IView GetView([NotNull] IRazorViewEngine viewEngine,
                             [NotNull] IRazorPage page,
                             bool isPartial)
        {
            var razorView = new RazorView(viewEngine, _pageActivator, _viewStartProvider, page, isPartial);
            return razorView;
        }
    }
}