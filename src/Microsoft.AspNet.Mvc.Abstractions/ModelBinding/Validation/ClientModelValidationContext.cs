// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Mvc.ModelBinding.Validation
{
    public class ClientModelValidationContext
    {
        public ClientModelValidationContext(
            ActionContext actionContext,
            ModelMetadata metadata,
            IModelMetadataProvider metadataProvider)
        {
            if (actionContext == null)
            {
                throw new ArgumentNullException(nameof(actionContext));
            }

            if (metadata == null)
            {
                throw new ArgumentNullException(nameof(metadata));
            }

            if (metadataProvider == null)
            {
                throw new ArgumentNullException(nameof(metadataProvider));
            }

            ActionContext = actionContext;
            ModelMetadata = metadata;
            MetadataProvider = metadataProvider;
        }

        public ActionContext ActionContext { get; }

        public ModelMetadata ModelMetadata { get; }

        public IModelMetadataProvider MetadataProvider { get; }
    }
}
