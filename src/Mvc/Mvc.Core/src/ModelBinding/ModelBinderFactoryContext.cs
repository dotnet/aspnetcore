// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    /// <summary>
    /// A context object for <see cref="ModelBinderFactory.CreateBinder"/>.
    /// </summary>
    public class ModelBinderFactoryContext
    {
        /// <summary>
        /// Gets or sets the <see cref="ModelBinding.BindingInfo"/>.
        /// </summary>
        public BindingInfo? BindingInfo { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="ModelMetadata"/>.
        /// </summary>
        public ModelMetadata Metadata { get; set; } = default!;

        /// <summary>
        /// Gets or sets the cache token. If <c>non-null</c> the resulting <see cref="IModelBinder"/>
        /// will be cached.
        /// </summary>
        public object? CacheToken { get; set; }
    }
}
