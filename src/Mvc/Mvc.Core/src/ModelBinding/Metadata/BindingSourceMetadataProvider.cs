// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using System;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Metadata
{
    /// <summary>
    /// Provides <see cref="BindingMetadata"/> for a <see cref="DefaultModelMetadata"/>.
    /// </summary>
    public class BindingSourceMetadataProvider : IBindingMetadataProvider
    {
        /// <summary>
        /// Creates a new <see cref="BindingSourceMetadataProvider"/> for the given <paramref name="type"/>.
        /// </summary>
        /// <param name="type">
        /// The <see cref="Type"/>. The provider sets <see cref="BindingSource"/> of the given <see cref="Type"/> or 
        /// anything assignable to the given <see cref="Type"/>. 
        /// </param>
        /// <param name="bindingSource">
        /// The <see cref="BindingSource"/> to assign to the given <paramref name="type"/>.
        /// </param>
        public BindingSourceMetadataProvider(Type type, BindingSource? bindingSource)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            Type = type;
            BindingSource = bindingSource;
        }


        /// <summary>
        /// The <see cref="Type"/>. The provider sets <see cref="BindingSource"/> of the given <see cref="Type"/> or 
        /// anything assignable to the given <see cref="Type"/>. 
        /// </summary>
        public Type Type { get; }

        /// <summary>
        /// The <see cref="BindingSource"/> to assign to the Type.
        /// </summary>
        public BindingSource? BindingSource { get; }

        /// <inheritdoc />
        public void CreateBindingMetadata(BindingMetadataProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (Type.IsAssignableFrom(context.Key.ModelType))
            {
                context.BindingMetadata.BindingSource = BindingSource;
            }
        }
    }
}
