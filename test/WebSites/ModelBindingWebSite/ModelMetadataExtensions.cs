// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc.ModelBinding;

namespace ModelBindingWebSite
{
    /// <summary>
    /// Extensions for <see cref="ModelMetadata"/>.
    /// </summary>
    public static class ModelMetadataExtensions
    {
        /// <summary>
        /// Gets the group name associated with given <paramref name="modelMetadata"/>.
        /// </summary>
        /// <param name="modelMetadata">The <see cref="ModelMetadata"/></param>
        /// <returns>Group name associated with given <paramref name="modelMetadata"/>.</returns>
        public static string GetGroupName(this ModelMetadata modelMetadata)
        {
            object groupName;
            modelMetadata.AdditionalValues.TryGetValue(AdditionalValuesMetadataProvider.GroupNameKey, out groupName);

            return groupName as string;
        }
    }
}