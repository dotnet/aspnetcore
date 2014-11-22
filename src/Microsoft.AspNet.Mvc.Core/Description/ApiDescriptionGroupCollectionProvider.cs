// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.Framework.DependencyInjection;

namespace Microsoft.AspNet.Mvc.Description
{
    /// <inheritdoc />
    public class ApiDescriptionGroupCollectionProvider : IApiDescriptionGroupCollectionProvider
    {
        private readonly IActionDescriptorsCollectionProvider _actionDescriptorCollectionProvider;
        private readonly INestedProviderManager<ApiDescriptionProviderContext> _apiDescriptionProvider;

        private ApiDescriptionGroupCollection _apiDescriptionGroups;

        /// <summary>
        /// Creates a new instance of <see cref="ApiDescriptionGroupCollectionProvider"/>.
        /// </summary>
        /// <param name="actionDescriptorCollectionProvider">
        /// The <see cref="IActionDescriptorsCollectionProvider"/>.
        /// </param>
        /// <param name="apiDescriptionProvider">
        /// The <see cref="INestedProviderManager{ApiDescriptionProviderContext}"/>.
        /// </param>
        public ApiDescriptionGroupCollectionProvider(
            IActionDescriptorsCollectionProvider actionDescriptorCollectionProvider,
            INestedProviderManager<ApiDescriptionProviderContext> apiDescriptionProvider)
        {
            _actionDescriptorCollectionProvider = actionDescriptorCollectionProvider;
            _apiDescriptionProvider = apiDescriptionProvider;
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
            _apiDescriptionProvider.Invoke(context);

            var groups = context.Results
                .GroupBy(d => d.GroupName)
                .Select(g => new ApiDescriptionGroup(g.Key, g.ToArray()))
                .ToArray();

            return new ApiDescriptionGroupCollection(groups, actionDescriptors.Version);
        }
    }
}