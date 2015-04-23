// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNet.Mvc.ApiExplorer
{
    /// <inheritdoc />
    public class ApiDescriptionGroupCollectionProvider : IApiDescriptionGroupCollectionProvider
    {
        private readonly IActionDescriptorsCollectionProvider _actionDescriptorCollectionProvider;
        private readonly IApiDescriptionProvider[] _apiDescriptionProviders;

        private ApiDescriptionGroupCollection _apiDescriptionGroups;

        /// <summary>
        /// Creates a new instance of <see cref="ApiDescriptionGroupCollectionProvider"/>.
        /// </summary>
        /// <param name="actionDescriptorCollectionProvider">
        /// The <see cref="IActionDescriptorsCollectionProvider"/>.
        /// </param>
        /// <param name="apiDescriptionProviders">
        /// The <see cref="IEnumerable{IApiDescriptionProvider}}"/>.
        /// </param>
        public ApiDescriptionGroupCollectionProvider(
            IActionDescriptorsCollectionProvider actionDescriptorCollectionProvider,
            IEnumerable<IApiDescriptionProvider> apiDescriptionProviders)
        {
            _actionDescriptorCollectionProvider = actionDescriptorCollectionProvider;
            _apiDescriptionProviders = apiDescriptionProviders.OrderBy(item => item.Order).ToArray();
        }

        /// <inheritdoc />
        public ApiDescriptionGroupCollection ApiDescriptionGroups
        {
            get
            {
                var actionDescriptors = _actionDescriptorCollectionProvider.ActionDescriptors;
                if (_apiDescriptionGroups == null || _apiDescriptionGroups.Version != actionDescriptors.Version)
                {
                    _apiDescriptionGroups = GetCollection(actionDescriptors);
                }

                return _apiDescriptionGroups;
            }
        }

        private ApiDescriptionGroupCollection GetCollection(ActionDescriptorsCollection actionDescriptors)
        {
            var context = new ApiDescriptionProviderContext(actionDescriptors.Items);

            foreach (var provider in _apiDescriptionProviders)
            {
                provider.OnProvidersExecuting(context);
            }

            for (var i = _apiDescriptionProviders.Length - 1; i >= 0; i--)
            {
                _apiDescriptionProviders[i].OnProvidersExecuted(context);
            }

            var groups = context.Results
                .GroupBy(d => d.GroupName)
                .Select(g => new ApiDescriptionGroup(g.Key, g.ToArray()))
                .ToArray();

            return new ApiDescriptionGroupCollection(groups, actionDescriptors.Version);
        }
    }
}