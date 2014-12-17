// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Mvc.OptionDescriptors
{
    /// <inheritdoc />
    public class DefaultInputFormattersProvider
        : OptionDescriptorBasedProvider<IInputFormatter>, IInputFormattersProvider
    {
        /// <summary>
        /// Initializes a new instance of the DefaultInputFormattersProvider class.
        /// </summary>
        /// <param name="options">An accessor to the <see cref="MvcOptions"/> configured for this application.</param>
        /// <param name="typeActivatorCache">As <see cref="ITypeActivatorCache"/> instance that creates an instance
        ///  of type <see cref="IInputFormatter"/>.</param>
        /// <param name="serviceProvider">A <see cref="IServiceProvider"/> instance that retrieves services from the
        /// service collection.</param>
        public DefaultInputFormattersProvider(IOptions<MvcOptions> optionsAccessor,
                                              ITypeActivatorCache typeActivatorCache,
                                              IServiceProvider serviceProvider)
            : base(optionsAccessor.Options.InputFormatters, typeActivatorCache, serviceProvider)
        {
        }

        /// <inheritdoc />
        public IReadOnlyList<IInputFormatter> InputFormatters
        {
            get
            {
                return Options;
            }
        }
    }
}