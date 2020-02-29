// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.ApiExplorer
{
    /// <summary>
    /// Represents group name metadata for an <c>ApiDescription</c>.
    /// </summary>
    public interface IApiDescriptionGroupNameProvider
    {
        /// <summary>
        /// The group name for the <c>ApiDescription</c> of the associated action or controller.
        /// </summary>
        string GroupName { get; }
    }
}