// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class ClientModelValidationContext
    {
        public ClientModelValidationContext([NotNull] ModelMetadata metadata,
                                            [NotNull] IModelMetadataProvider metadataProvider,
                                            [NotNull] IServiceProvider requestServices)
        {
            ModelMetadata = metadata;
            MetadataProvider = metadataProvider;
            RequestServices = requestServices;
        }

        public ModelMetadata ModelMetadata { get; }

        public IModelMetadataProvider MetadataProvider { get; }

        public IServiceProvider RequestServices { get; }
    }
}
