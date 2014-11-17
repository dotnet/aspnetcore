// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc.Logging;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Logging;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Default implementation for ActionDescriptors.
    /// This implementation caches the results at first call, and is not responsible for updates.
    /// </summary>
    public class DefaultActionDescriptorsCollectionProvider : IActionDescriptorsCollectionProvider
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;
        private ActionDescriptorsCollection _collection;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultActionDescriptorsCollectionProvider" /> class.
        /// </summary>
        /// <param name="serviceProvider">The application IServiceProvider.</param>
        public DefaultActionDescriptorsCollectionProvider(IServiceProvider serviceProvider, ILoggerFactory factory)
        {
            _serviceProvider = serviceProvider;
            _logger = factory.Create<DefaultActionDescriptorsCollectionProvider>();
        }

        /// <summary>
        /// Returns a cached collection of <see cref="ActionDescriptor" />.
        /// </summary>
        public ActionDescriptorsCollection ActionDescriptors
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

        private ActionDescriptorsCollection GetCollection()
        {
            var actionDescriptorProvider =
                _serviceProvider.GetRequiredService<INestedProviderManager<ActionDescriptorProviderContext>>();
            var actionDescriptorProviderContext = new ActionDescriptorProviderContext();

            actionDescriptorProvider.Invoke(actionDescriptorProviderContext);

            if (_logger.IsEnabled(LogLevel.Verbose))
            {
                foreach (var actionDescriptor in actionDescriptorProviderContext.Results)
                {
                    _logger.WriteVerbose(new ActionDescriptorValues(actionDescriptor));
                }
            }

            return new ActionDescriptorsCollection(actionDescriptorProviderContext.Results, 0);
        }
    }
}