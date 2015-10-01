// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Mvc.ModelBinding.Validation
{
    public class ClientModelValidationContext
    {
        public ClientModelValidationContext(
            ModelMetadata metadata,
            IModelMetadataProvider metadataProvider,
            IServiceProvider requestServices)
        {
            if (metadata == null)
            {
                throw new ArgumentNullException(nameof(metadata));
            }

            if (metadataProvider == null)
            {
                throw new ArgumentNullException(nameof(metadataProvider));
            }

            if (requestServices == null)
            {
                throw new ArgumentNullException(nameof(requestServices));
            }

            ModelMetadata = metadata;
            MetadataProvider = metadataProvider;
            RequestServices = requestServices;
        }

        public ModelMetadata ModelMetadata { get; }

        public IModelMetadataProvider MetadataProvider { get; }

        public IServiceProvider RequestServices { get; }
    }
}
