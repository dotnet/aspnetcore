// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Metadata
{
    /// <summary>
    /// An <see cref="IBindingMetadataProvider"/> for <see cref="DataMemberAttribute.IsRequired"/>.
    /// </summary>
    public class DataMemberRequiredBindingMetadataProvider : IBindingMetadataProvider
    {
        /// <inheritdoc />
        public void CreateBindingMetadata(BindingMetadataProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            // Types cannot be required; only properties can
            if (context.Key.MetadataKind != ModelMetadataKind.Property)
            {
                return;
            }

            if (context.BindingMetadata.IsBindingRequired)
            {
                // This value is already required, no need to look at attributes.
                return;
            }

            var dataMemberAttribute = context
                .PropertyAttributes
                .OfType<DataMemberAttribute>()
                .FirstOrDefault();
            if (dataMemberAttribute == null || !dataMemberAttribute.IsRequired)
            {
                return;
            }

            // isDataContract == true iff the container type has at least one DataContractAttribute
            var containerType = context.Key.ContainerType.GetTypeInfo();
            var isDataContract = containerType.IsDefined(typeof(DataContractAttribute));
            if (isDataContract)
            {
                // We don't need to add a validator, just to set IsRequired = true. The validation
                // system will do the right thing.
                context.BindingMetadata.IsBindingRequired = true;
            }
        }
    }
}