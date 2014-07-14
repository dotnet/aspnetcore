// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Mvc
{
    /// <inheritdoc />
    public class DefaultOutputFormattersProvider : IOutputFormattersProvider
    {
        private readonly List<OutputFormatterDescriptor> _descriptors;
        private readonly ITypeActivator _typeActivator;
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Initializes a new instance of the DefaultOutputFormattersProvider class.
        /// </summary>
        /// <param name="options">An accessor to the <see cref="MvcOptions"/> configured for this application.</param>
        /// <param name="typeActivator">An <see cref="ITypeActivator"/> instance used to instantiate types.</param>
        /// <param name="serviceProvider">A <see cref="IServiceProvider"/> instance that retrieves services from the 
        /// service collection.</param>
        public DefaultOutputFormattersProvider(IOptionsAccessor<MvcOptions> options,
                                           ITypeActivator typeActivator,
                                           IServiceProvider serviceProvider)
        {
            _descriptors = options.Options.OutputFormatters;
            _typeActivator = typeActivator;
            _serviceProvider = serviceProvider;
        }

        /// <inheritdoc />
        public IReadOnlyList<IOutputFormatter> OutputFormatters
        {
            get
            {
                var outputFormatters = new List<IOutputFormatter>();
                foreach (var descriptor in _descriptors)
                {
                    var formatter = descriptor.OutputFormatter;
                    if (formatter == null)
                    {
                        formatter = (IOutputFormatter)_typeActivator.CreateInstance(_serviceProvider, 
                                                                             descriptor.OutputFormatterType);
                    }

                    outputFormatters.Add(formatter);
                }

                return outputFormatters;
            }
        }
    }
}