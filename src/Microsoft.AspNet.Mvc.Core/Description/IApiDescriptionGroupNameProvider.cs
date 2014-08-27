// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Mvc.Description
{
    /// <summary>
    /// Represents group name metadata for an <see cref="ApiDescription"/>.
    /// </summary>
    public interface IApiDescriptionGroupNameProvider
    {
        /// <summary>
        /// The group name for the <see cref="ApiDescription"/> of the associated action or controller.
        /// </summary>
        string GroupName { get; }
    }
}