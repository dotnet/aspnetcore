// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    /// <summary>
    /// Metadata which specificies the data source for model binding.
    /// </summary>
    public interface IBindingSourceMetadata : IBinderMetadata
    {
        /// <summary>
        /// Gets the <see cref="BindingSource"/>. 
        /// </summary>
        /// <remarks>
        /// The <see cref="BindingSource"/> is metadata which can be used to determine which data
        /// sources are valid for model binding of a property or parameter.
        /// </remarks>
        BindingSource BindingSource { get; }
    }
}