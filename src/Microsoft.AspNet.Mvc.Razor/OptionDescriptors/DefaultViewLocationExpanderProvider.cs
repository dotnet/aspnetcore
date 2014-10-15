// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Mvc.OptionDescriptors;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Mvc.Razor.OptionDescriptors
{
    /// <inheritdoc />
    public class DefaultViewLocationExpanderProvider :
        OptionDescriptorBasedProvider<IViewLocationExpander>, IViewLocationExpanderProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultViewLocationExpanderProvider"/> class.
        /// </summary>
        /// <param name="options">An accessor to the <see cref="MvcOptions"/> configured for this application.</param>
        /// <param name="typeActivator">An <see cref="ITypeActivator"/> instance used to instantiate types.</param>
        /// <param name="serviceProvider">A <see cref="IServiceProvider"/> instance that retrieves services from the 
        /// service collection.</param>
        public DefaultViewLocationExpanderProvider(
            IOptions<RazorViewEngineOptions> optionsAccessor,
            ITypeActivator typeActivator,
            IServiceProvider serviceProvider)
            : base(optionsAccessor.Options.ViewLocationExpanders, typeActivator, serviceProvider)
        {
        }

        /// <inheritdoc />
        public IReadOnlyList<IViewLocationExpander> ViewLocationExpanders
        {
            get { return Options; }
        }
    }
}