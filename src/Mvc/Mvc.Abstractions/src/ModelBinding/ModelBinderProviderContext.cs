// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    /// <summary>
    /// A context object for <see cref="IModelBinderProvider.GetBinder"/>.
    /// </summary>
    public abstract class ModelBinderProviderContext
    {
        /// <summary>
        /// Creates an <see cref="IModelBinder"/> for the given <paramref name="metadata"/>.
        /// </summary>
        /// <param name="metadata">The <see cref="ModelMetadata"/> for the model.</param>
        /// <returns>An <see cref="IModelBinder"/>.</returns>
        public abstract IModelBinder CreateBinder(ModelMetadata metadata);

        /// <summary>
        /// Creates an <see cref="IModelBinder"/> for the given <paramref name="metadata"/>
        /// and <paramref name="bindingInfo"/>.
        /// </summary>
        /// <param name="metadata">The <see cref="ModelMetadata"/> for the model.</param>
        /// <param name="bindingInfo">The <see cref="BindingInfo"/> that should be used
        /// for creating the binder.</param>
        /// <returns>An <see cref="IModelBinder"/>.</returns>
        public virtual IModelBinder CreateBinder(ModelMetadata metadata, BindingInfo bindingInfo)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Gets the <see cref="BindingInfo"/>.
        /// </summary>
        public abstract BindingInfo BindingInfo { get; }

        /// <summary>
        /// Gets the <see cref="ModelMetadata"/>.
        /// </summary>
        public abstract ModelMetadata Metadata { get; }

        /// <summary>
        /// Gets the <see cref="IModelMetadataProvider"/>.
        /// </summary>
        public abstract IModelMetadataProvider MetadataProvider { get; }

        /// <summary>
        /// Gets the <see cref="IServiceProvider"/>.
        /// </summary>
        public virtual IServiceProvider Services { get; }
    }
}