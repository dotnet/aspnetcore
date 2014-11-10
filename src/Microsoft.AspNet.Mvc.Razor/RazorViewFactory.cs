// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc.Rendering;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class RazorViewFactory : IRazorViewFactory
    {
        private readonly IRazorPageActivator _pageActivator;
        private readonly IRazorPageFactory _pageFactory;
        private readonly IViewStartProvider _viewStartProvider;

        /// <summary>
        /// Initializes a new instance of RazorViewFactory
        /// </summary>
        /// <param name="pageFactory">The page factory used to instantiate layout and _ViewStart pages.</param>
        /// <param name="pageActivator">The <see cref="IRazorPageActivator"/> used to activate pages.</param>
        /// <param name="viewStartProvider">The <see cref="IViewStartProvider"/> used for discovery of _ViewStart
        /// pages</param>
        public RazorViewFactory(IRazorPageFactory pageFactory,
                         IRazorPageActivator pageActivator,
                         IViewStartProvider viewStartProvider)
        {
            _pageFactory = pageFactory;
            _pageActivator = pageActivator;
            _viewStartProvider = viewStartProvider;
        }

        /// <inheritdoc />
        public IView GetView([NotNull] IRazorPage page, bool isPartial)
        {
            var razorView = new RazorView(_pageFactory, _pageActivator, _viewStartProvider);

            razorView.Contextualize(page, isPartial);

            return razorView;
        }
    }
}