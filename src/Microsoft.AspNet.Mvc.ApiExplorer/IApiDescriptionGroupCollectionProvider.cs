// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Mvc.ApiExplorer
{
    /// <summary>
    /// Provides access to a collection of <see cref="ApiDescriptionGroup"/>.
    /// </summary>
    public interface IApiDescriptionGroupCollectionProvider
    {
        /// <summary>
        /// Gets a collection of <see cref="ApiDescriptionGroup"/>.
        /// </summary>
        ApiDescriptionGroupCollection ApiDescriptionGroups { get; }
    }
}