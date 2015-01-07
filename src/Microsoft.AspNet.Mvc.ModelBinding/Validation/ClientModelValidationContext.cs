// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class ClientModelValidationContext
    {
        public ClientModelValidationContext([NotNull] ModelMetadata metadata,
                                            [NotNull] IModelMetadataProvider metadataProvider)
        {
            ModelMetadata = metadata;
            MetadataProvider = metadataProvider;
        }

        public ModelMetadata ModelMetadata { get; private set; }

        public IModelMetadataProvider MetadataProvider { get; private set; }
    }
}
