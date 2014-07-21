// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Mvc.OptionDescriptors
{
    /// <summary>
    /// Provides a default implementation for instantiating options from a sequence of 
    /// <see cref="OptionDescriptor{TOption}"/>.
    /// </summary>
    /// <typeparam name="TOption">The type of the option.</typeparam>
    public abstract class OptionDescriptorBasedProvider<TOption>
    {
        private readonly IEnumerable<OptionDescriptor<TOption>> _optionDescriptors;
        private readonly ITypeActivator _typeActivator;
        private readonly IServiceProvider _serviceProvider;

        public OptionDescriptorBasedProvider(
            [NotNull] IEnumerable<OptionDescriptor<TOption>> optionDescriptors,
            [NotNull] ITypeActivator typeActivator,
            [NotNull] IServiceProvider serviceProvider)
        {
            _optionDescriptors = optionDescriptors;
            _typeActivator = typeActivator;
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Gets an activated sequence of <typeparamref name="TOption"/> instance.
        /// </summary>
        protected IReadOnlyList<TOption> Options
        {
            get
            {
                var result = new List<TOption>();
                foreach (var descriptor in _optionDescriptors)
                {
                    var instance = descriptor.Instance;
                    if (instance == null)
                    {
                        instance = (TOption)_typeActivator.CreateInstance(_serviceProvider,
                                                                          descriptor.OptionType);
                    }

                    result.Add(instance);
                }

                return result;
            }
        }
    }
}