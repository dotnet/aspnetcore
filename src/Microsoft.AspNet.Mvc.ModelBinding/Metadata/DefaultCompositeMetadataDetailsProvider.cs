// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding.Metadata
{
    /// <summary>
    /// A default implementation of <see cref="ICompositeMetadataDetailsProvider"/>.
    /// </summary>
    public class DefaultCompositeMetadataDetailsProvider : ICompositeMetadataDetailsProvider
    {
        private readonly IEnumerable<IMetadataDetailsProvider> _providers;

        /// <summary>
        /// Creates a new <see cref="DefaultCompositeMetadataDetailsProvider"/>.
        /// </summary>
        /// <param name="providers">The set of <see cref="IMetadataDetailsProvider"/> instances.</param>
        public DefaultCompositeMetadataDetailsProvider(IEnumerable<IMetadataDetailsProvider> providers)
        {
            _providers = providers;
        }

        /// <inheritdoc />
        public virtual void GetBindingMetadata([NotNull] BindingMetadataProviderContext context)
        {
            foreach (var provider in _providers.OfType<IBindingMetadataProvider>())
            {
                provider.GetBindingMetadata(context);
            }
        }

        /// <inheritdoc />
        public virtual void GetDisplayMetadata([NotNull] DisplayMetadataProviderContext context)
        {
            foreach (var provider in _providers.OfType<IDisplayMetadataProvider>())
            {
                provider.GetDisplayMetadata(context);
            }
        }

        /// <inheritdoc />
        public virtual void GetValidationMetadata([NotNull] ValidationMetadataProviderContext context)
        {
            foreach (var provider in _providers.OfType<IValidationMetadataProvider>())
            {
                provider.GetValidationMetadata(context);
            }
        }
    }
}