// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels
{
    /// <summary>
    /// A model for ApiExplorer properties associated with a controller or action.
    /// </summary>
    public class ApiExplorerModel
    {
        /// <summary>
        /// Creates a new <see cref="ApiExplorerModel"/>.
        /// </summary>
        public ApiExplorerModel()
        {
        }

        /// <summary>
        /// Creates a new <see cref="ApiExplorerModel"/> with properties copied from <paramref name="other"/>.
        /// </summary>
        /// <param name="other">The <see cref="ApiExplorerModel"/> to copy.</param>
        public ApiExplorerModel(ApiExplorerModel other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            GroupName = other.GroupName;
            IsVisible = other.IsVisible;
        }

        /// <summary>
        /// If <c>true</c>, <c>APIExplorer.ApiDescription</c> objects will be created for the associated
        /// controller or action.
        /// </summary>
        /// <remarks>
        /// Set this value to configure whether or not the associated controller or action will appear in ApiExplorer.
        /// </remarks>
        public bool? IsVisible { get; set; }

        /// <summary>
        /// The value for <c>APIExplorer.ApiDescription.GroupName</c> of
        /// <c>APIExplorer.ApiDescription</c> objects created for the associated controller or action.
        /// </summary>
        public string GroupName { get; set; }
    }
}