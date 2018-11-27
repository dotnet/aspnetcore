// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    /// <summary>
    /// Metadata which specifies the data source for model binding.
    /// </summary>
    public interface IBindingSourceMetadata
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