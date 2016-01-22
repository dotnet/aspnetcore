// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    /// <summary>
    /// Default implementation of <see cref="IActionDescriptorCollectionProvider"/>.
    /// This implementation caches the results at first call, and is not responsible for updates.
    /// </summary>
    public class ActionDescriptorCollectionProvider : IActionDescriptorCollectionProvider
    {
        private readonly IServiceProvider _serviceProvider;
        private ActionDescriptorCollection _collection;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActionDescriptorCollectionProvider" /> class.
        /// </summary>
        /// <param name="serviceProvider">The application IServiceProvider.</param>
        public ActionDescriptorCollectionProvider(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Returns a cached collection of <see cref="ActionDescriptor" />.
        /// </summary>
        public ActionDescriptorCollection ActionDescriptors
        {
            get
            {
                if (_collection == null)
                {
                    _collection = GetCollection();
                }

                return _collection;
            }
        }

        private ActionDescriptorCollection GetCollection()
        {
            var providers =
                _serviceProvider.GetServices<IActionDescriptorProvider>()
                                .OrderBy(p => p.Order)
                                .ToArray();

            var context = new ActionDescriptorProviderContext();

            foreach (var provider in providers)
            {
                provider.OnProvidersExecuting(context);
            }

            for (var i = providers.Length - 1; i >= 0; i--)
            {
                providers[i].OnProvidersExecuted(context);
            }

            return new ActionDescriptorCollection(
                new ReadOnlyCollection<ActionDescriptor>(context.Results), 0);
        }
    }
}