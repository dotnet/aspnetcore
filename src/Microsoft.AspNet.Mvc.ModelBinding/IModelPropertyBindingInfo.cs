// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Represents an entity which has binding information for a model.
    /// </summary>
    public interface IModelPropertyBindingInfo
    {
        /// <summary>
        /// Comma separated set of properties which are to be excluded during model binding.
        /// </summary>
        string Exclude { get; }

        /// <summary>
        /// Comma separated set of properties which are to be included during model binding.
        /// </summary>
        string Include { get; }
    }
}
